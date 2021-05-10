using System;
using System.Security.Cryptography;
using System.Text;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TcpListenerProject.OBSApi
{
    public partial class OBSWebsocket
    {
        #region Events
        /// <summary>
        /// Triggered when OBS exits
        /// </summary>
        public event EventHandler OBSExit;

        /// <summary>
        /// Triggered when connected successfully to an obs-websocket server
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Triggered when disconnected from an obs-websocket server
        /// </summary>
        public event EventHandler Disconnected;
        #endregion

        /// <summary>
        /// WebSocket request timeout, represented as a TimeSpan object
        /// </summary>
        public TimeSpan WSTimeout
        {
            get
            {
                if (WSConnection != null)
                    return WSConnection.WaitTime;
                else
                    return _pWSTimeout;
            }
            set
            {
                _pWSTimeout = value;

                if (WSConnection != null)
                    WSConnection.WaitTime = _pWSTimeout;
            }
        }
        private TimeSpan _pWSTimeout = TimeSpan.FromSeconds(10);

        // Random should never be created inside a function
        private static readonly Random random = new Random();

        /// <summary>
        /// Current connection state
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return (WSConnection != null && WSConnection.IsAlive);
            }
        }

        /// <summary>
        /// Underlying WebSocket connection to an obs-websocket server. Value is null when disconnected.
        /// </summary>
        public WebSocket WSConnection { get; private set; }

        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> responseHandlers;

        /// <summary>
        /// Constructor
        /// </summary>
        public OBSWebsocket()
        {
            responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
        }

        /// <summary>
        /// Connect this instance to the specified URL, and authenticate (if needed) with the specified password
        /// </summary>
        /// <param name="url">Server URL in standard URL format</param>
        /// <param name="password">Server password</param>
        public void Connect(string url, string password)
        {
            if (WSConnection != null && WSConnection.IsAlive)
            {
                Disconnect();
            }

            WSConnection = new WebSocket(url)
            {
                WaitTime = _pWSTimeout
            };
            WSConnection.OnMessage += WebsocketMessageHandler;
            WSConnection.OnClose += (s, e) =>
            {
                Disconnected?.Invoke(this, e);
            };
            WSConnection.Connect();

            if (!WSConnection.IsAlive)
                return;

            OBSAuthInfo authInfo = GetAuthInfo();

            if (authInfo.AuthRequired)
                Authenticate(password, authInfo);

            Connected?.Invoke(this, null);
        }

        /// <summary>
        /// Disconnect this instance from the server
        /// </summary>
        public void Disconnect()
        {
            if (WSConnection != null)
            {
                // Attempt to both close and dispose the existing connection
                try
                {
                    WSConnection.Close();
                    ((IDisposable)WSConnection).Dispose();
                }
                catch { }
                WSConnection = null;
            }
            
            var unusedHandlers = responseHandlers.ToArray();
            responseHandlers.Clear();
            foreach (var cb in unusedHandlers)
            {
                var tcs = cb.Value;
                tcs.TrySetCanceled();
            }
        }

        // This callback handles incoming JSON messages and determines if it's
        // a request response or an event ("Update" in obs-websocket terminology)
        private void WebsocketMessageHandler(object sender, MessageEventArgs e)
        {
            if (!e.IsText)
                return;

            JObject body = JObject.Parse(e.Data);

            if (body["message-id"] != null)
            {
                // Handle a request :
                // Find the response handler based on
                // its associated message ID
                string msgID = (string)body["message-id"];

                if (responseHandlers.TryRemove(msgID, out TaskCompletionSource<JObject> handler))
                {
                    // Set the response body as Result and notify the request sender
                    handler.SetResult(body);
                }
            }
            /*else if (body["update-type"] != null)
            {
                // Handle an event
                string eventType = body["update-type"].ToString();
                ProcessEventType(eventType, body);
            }*/
        }

        /// <summary>
        /// Sends a message to the websocket API with the specified request type and optional parameters
        /// </summary>
        /// <param name="requestType">obs-websocket request type, must be one specified in the protocol specification</param>
        /// <param name="additionalFields">additional JSON fields if required by the request type</param>
        /// <returns>The server's JSON response as a JObject</returns>
        public JObject SendRequest(string requestType, JObject additionalFields = null)
        {
            string messageID;

            // Build the bare-minimum body for a request
            var body = new JObject
            {
                { "request-type", requestType }
            };

            // Add optional fields if provided
            if (additionalFields != null)
            {
                _ = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union
                };

                body.Merge(additionalFields);
            }

            // Prepare the asynchronous response handler
            var tcs = new TaskCompletionSource<JObject>();
            do
            {
                // Generate a random message id
                messageID = NewMessageID();
                if (responseHandlers.TryAdd(messageID, tcs))
                {
                    body.Add("message-id", messageID);
                    break;
                }
                // Message id already exists, retry with a new one.
            } while (true);
            // Send the message and wait for a response
            // (received and notified by the websocket response handler)
            WSConnection.Send(body.ToString());
            tcs.Task.Wait();

            if (tcs.Task.IsCanceled)
                throw new ErrorResponseException("Request canceled");

            // Throw an exception if the server returned an error.
            // An error occurs if authentication fails or one if the request body is invalid.
            var result = tcs.Task.Result;

            if ((string)result["status"] == "error")
                throw new ErrorResponseException((string)result["error"]);

            return result;
        }

        /// <summary>
        /// Request authentication data. You don't have to call this manually.
        /// </summary>
        /// <returns>Authentication data in an <see cref="OBSAuthInfo"/> object</returns>
        public OBSAuthInfo GetAuthInfo()
        {
            JObject response = SendRequest("GetAuthRequired");
            return new OBSAuthInfo(response);
        }

        /// <summary>
        /// Authenticates to the Websocket server using the challenge and salt given in the passed <see cref="OBSAuthInfo"/> object
        /// </summary>
        /// <param name="password">User password</param>
        /// <param name="authInfo">Authentication data</param>
        /// <returns>true if authentication succeeds, false otherwise</returns>
        public bool Authenticate(string password, OBSAuthInfo authInfo)
        {
            string secret = HashEncode(password + authInfo.PasswordSalt);
            string authResponse = HashEncode(secret + authInfo.Challenge);

            var requestFields = new JObject
            {
                { "auth", authResponse }
            };

            try
            {
                // Throws ErrorResponseException if auth fails
                SendRequest("Authenticate", requestFields);
            }
            catch (ErrorResponseException)
            {
                throw new AuthFailureException();
            }

            return true;
        }

        /// <summary>
        /// Update message handler
        /// </summary>
        /// <param name="eventType">Value of "event-type" in the JSON body</param>
        /// <param name="body">full JSON message body</param>
        protected void ProcessEventType(string eventType, JObject body)
        {
            switch (eventType)
            {
                case "Exiting":
                    OBSExit?.Invoke(this, EventArgs.Empty);
                    break;

                default:
                        var message = $"Unsupported Event: {eventType}\n{body}";
                        Console.WriteLine(message);
                        Debug.WriteLine(message);
                        break;
            }
        }

        /// <summary>
        /// Encode a Base64-encoded SHA-256 hash
        /// </summary>
        /// <param name="input">source string</param>
        /// <returns></returns>
        protected string HashEncode(string input)
        {
            using (var sha256 = new SHA256Managed())
            {

                byte[] textBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = sha256.ComputeHash(textBytes);

                return System.Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Generate a message ID
        /// </summary>
        /// <param name="length">(optional) message ID length</param>
        /// <returns>A random string of alphanumerical characters</returns>
        protected string NewMessageID(int length = 16)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string result = "";
            for (int i = 0; i < length; i++)
            {
                int index = random.Next(0, pool.Length - 1);
                result += pool[index];
            }

            return result;
        }
    }
}
