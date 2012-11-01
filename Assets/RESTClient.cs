using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using JsonFx.Json;
using UnityEngine;

public class DeserialisationException : Exception
{
    public DeserialisationException(string endpoint, string response) : base("Failed to deserialise response from " + endpoint + ": " + response) { }
}

public class WWWRequestError : Exception
{
    public WWWRequestError(string error) : base(error) { }
}

/// <summary>
/// Make asynchronous requests to a JSON REST API.
/// </summary>
public class RESTClient : MonoBehaviour
{
    /// <summary>
    /// How long an operation can be pending before an error is raised
    /// </summary>
    [SerializeField]
    private int m_operationTimeoutMilliseconds = 15000;

    /// <summary>
    /// Describes a pending operations
    /// </summary>
    private class PendingOperation
    {
        /// <summary>
        /// What to run if the opeartion successfully completes
        /// </summary>
        public Action<string> OnCompletion
        {
            get;
            private set;
        }

        /// <summary>
        /// What to run if the opeartion fails
        /// </summary>
        public Action<Exception> OnException
        {
            get;
            private set;
        }

        /// <summary>
        /// When the operation was started
        /// </summary>
        public DateTime StartTime
        {
            get;
            private set;
        }

        /// <summary>
        /// How long the operation has to complete
        /// </summary>
        public TimeSpan Timeout
        {
            get;
            private set;
        }

        /// <summary>
        /// The Unity WWW object responsible for handling this request
        /// </summary>
        public WWW WWW
        {
            get;
            private set;
        }

        public PendingOperation(WWW www, Action<string> completionHandler, Action<Exception> exceptionHandler, TimeSpan timeout)
        {
            OnCompletion = completionHandler;
            OnException = exceptionHandler;
            Timeout = timeout;
            StartTime = DateTime.Now;
            WWW = www;
        }
    }

    /// <summary>
    /// Operations that are currently awaiting a response
    /// </summary>
    private List<PendingOperation> m_pendingOperations = new List<PendingOperation>();

    /// <summary>
    /// JSON serialiser. Could be swapped out for any other JSON serialiser
    /// </summary>
    private JsonReader m_reader = new JsonReader();

    public void Update()
    {
        for (int i = m_pendingOperations.Count - 1; i >= 0; i--)
        {
            PendingOperation pendingOperation = m_pendingOperations[i];

            //Check if a response has arrived
            if (pendingOperation.WWW.isDone)
            {
                try
                {
                    if (string.IsNullOrEmpty(pendingOperation.WWW.error))
                    {
                        //Run the completion handler if there was no error
                        pendingOperation.OnCompletion(pendingOperation.WWW.text);
                    }
                    else
                    {
                        //otherwiser run the exception handler
                        pendingOperation.OnException(new WWWRequestError(pendingOperation.WWW.error));
                    }
                }
                catch (Exception ex)
                {
                    pendingOperation.OnException(ex);
                }

                m_pendingOperations[i].WWW.Dispose();
                m_pendingOperations.RemoveAt(i);
            }
            else if (DateTime.Now - pendingOperation.StartTime > pendingOperation.Timeout)
            {
                //Handle operation time out
                pendingOperation.OnException(new TimeoutException(pendingOperation.WWW.url));

                m_pendingOperations[i].WWW.Dispose();
                m_pendingOperations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Make a GET request
    /// </summary>
    /// <typeparam name="T">The response type expected</typeparam>
    /// <param name="endpoint">The URL to make the GET request to</param>
    public Future<T> Get<T>(Uri endpoint)
    {
        return Get<T>(endpoint, null);
    }

    /// <summary>
    /// Make a GET request
    /// </summary>
    /// <typeparam name="T">The response type expected</typeparam>
    /// <param name="endpoint">The URL to make the GET request to</param>
    /// <param name="parameters">Any additional parameters to add to the url that receives the GET request.</param>
    public Future<T> Get<T>(Uri endpoint, object parameters)
    {
        Future<T> future = new Future<T>();

        string finalEndpoint = parameters != null ? BuildUrlString(endpoint, parameters) : endpoint.ToString();
        WWW www = new WWW(finalEndpoint);

        PendingOperation operation = new PendingOperation
        (
            /* WWW */
            www,

            /* Completion handler */
            (response) =>
            {
                RequestCompletionHandler<T>(future, finalEndpoint, response);
            },

            /* Exception handler */
            (exception) =>
            {
                future.SetException(exception);
            },

            /* Timeout */
            TimeSpan.FromMilliseconds(m_operationTimeoutMilliseconds)
        );

        m_pendingOperations.Add(operation);

        return future;
    }

    private void RequestCompletionHandler<T>(Future<T> future, string finalEndpoint, string response)
    {
        try
        {
            //Try to deserialise the JSON response to the expected response type
            T result = default(T);
            bool deserialised = false;
            try
            {
                result = Deserialise<T>(response);
                deserialised = true;
            }
            catch
            {
                //If this goes wrong, set the future as failed
                future.SetException(new DeserialisationException(finalEndpoint, response));
            }

            if (deserialised)
            {
                //If it goes OK, set the future as completed!
                future.SetResult(result);
            }
        }
        catch (Exception ex)
        {
            future.SetException(ex);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    public Future<T> Post<T>(Uri endpoint, object parameters)
    {
        throw new NotImplementedException();
    }

    private T Deserialise<T>(string json)
    {
        return m_reader.Read<T>(json);
    }

    private static string BuildUrlString(Uri endpoint, object parameters)
    {
        StringBuilder url = new StringBuilder(endpoint.ToString());

        //Check to see whether we need to create the query component of the URL 
        if (endpoint.Query.Length == 0)
        {
            url.Append('?');
        }
        //Or just add to an existing one
        else
        {
            url.Append('&');
        }

        //Take all Name->Values from the parameters object and add them on to the URL.
        //E.G. http://my.endpoint.com/api/?name=value&name2=value2&name3=value3, etc...
        PropertyInfo[] properties = parameters.GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo pi = properties[i];
            url.Append(pi.Name);
            url.Append('=');
            url.Append(pi.GetValue(parameters, null).ToString());
            if (i < properties.Length - 1)
            {
                url.Append('&');
            }
        }

        return url.ToString();
    }
}
