using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ThreeDToolkit
{
    public class HttpRequest
    {
        private string method;
        private UriBuilder uriBuilder;
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private string requestBody;
        private IAsyncResult asyncResult;

        public HttpRequest(string baseUri)
        {
            uriBuilder = new UriBuilder(baseUri);
        }

        public HttpRequest(Uri uri)
        {
            uriBuilder = new UriBuilder(uri);
        }

        public HttpRequest()
        {
        }

        public HttpRequest Get(string uri)
        {
            return this.Request("GET", uri);
        }

        public HttpRequest Post(string uri)
        {
            return this.Request("POST", uri);
        }

        public HttpRequest Put(string uri)
        {
            return this.Request("PUT", uri);
        }

        public HttpRequest Delete(string uri)
        {
            return this.Request("DELETE", uri);
        }

        public HttpRequest Patch(string uri)
        {
            return this.Request("PATCH", uri);
        }

        public HttpRequest Header<TVal>(string key, TVal value)
        {
            this.headers[key] = value.ToString();

            return this;
        }

        public HttpRequest Header<TVal>(IDictionary<string, TVal> headers)
        {
            foreach (var header in headers)
            {
                this.headers[header.Key] = header.Value.ToString();
            }

            return this;
        }

        public HttpRequest Data(string data)
        {
            this.requestBody = data;

            return this;
        }

        public HttpRequest AppendQuery(string qs)
        {
            if (uriBuilder == null)
            {
                uriBuilder = new UriBuilder();
            }

            if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
                uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + qs;
            else
                uriBuilder.Query = qs;

            return this;
        }

        public HttpRequest AppendQuery<TValue>(string key, TValue value)
        {
            return this.AppendQuery(key + "=" + value.ToString());
        }

        public HttpRequest Request(string method, string uri)
        {
            if (uriBuilder == null)
            {
                uriBuilder = new UriBuilder(uri);
            }
            else
            {
                // treat existing as baseUri and use uri as path
                uriBuilder.Path = uri;
            }

            this.method = method;

            return this;
        }

        private HttpWebRequest InternalBuildRequest()
        {
            var req = HttpWebRequest.Create(this.uriBuilder.Uri) as HttpWebRequest;

            req.Method = this.method;

            foreach (var header in this.headers)
            {
                req.Headers.Set(header.Key, header.Value);
            }

            if (this.method.ToUpper() != "GET" && !string.IsNullOrEmpty(this.requestBody))
            {
                using (var sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(this.requestBody);
                }
            }

            return req;
        }

        public HttpWebResponse Sync(int timeoutMs)
        {
            var req = this.InternalBuildRequest();
            req.Timeout = timeoutMs;
            return req.GetResponse() as HttpWebResponse;
        }

        public IAsyncResult Done(Action<HttpWebResponse> onSuccess, Action<Exception> onError = null)
        {
            try
            {
                var req = this.InternalBuildRequest();

                return asyncResult = req.BeginGetResponse(Handle_Done, new AsyncHandleDoneState()
                {
                    Req = req,
                    Success = onSuccess,
                    Error = onError
                });
            }
            catch (Exception ex)
            {
                onError(ex);
            }

            return null;
        }

        private void Handle_Done(IAsyncResult state)
        {
            var data = (AsyncHandleDoneState)state.AsyncState;

            try
            {
                var res = data.Req.EndGetResponse(asyncResult) as HttpWebResponse;
                data.Success(res);
            }
            catch (Exception ex)
            {
                data.Error(ex);
            }
        }

        private struct AsyncHandleDoneState
        {
            public HttpWebRequest Req;
            public Action<HttpWebResponse> Success;
            public Action<Exception> Error;
        }
    }
}
