using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Diagnostics.Contracts;

namespace Microsoft.AspNet.OData.Results
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConflictedODataResult<T> : IHttpActionResult
    {
        private readonly NegotiatedContentResult<T> _innerResult;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="controller"></param>
        public ConflictedODataResult(T entity, ApiController controller)
        : this(new NegotiatedContentResult<T>(HttpStatusCode.PreconditionFailed, CheckNull(entity), controller))
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="contentNegotiator"></param>
        /// <param name="request"></param>
        /// <param name="formatters"></param>
        public ConflictedODataResult(T entity, IContentNegotiator contentNegotiator, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
            : this(new NegotiatedContentResult<T>(HttpStatusCode.PreconditionFailed, CheckNull(entity), contentNegotiator, request, formatters))
        {
        }

        private ConflictedODataResult(NegotiatedContentResult<T> innerResult)
        {
            Contract.Assert(innerResult != null);
            _innerResult = innerResult;
        }

        /// <summary>
        /// Gets the entity that was updated.
        /// </summary>
        public T Entity => _innerResult.Content;

        /// <summary>
        /// Gets the content negotiator to handle content negotiation.
        /// </summary>
        public IContentNegotiator ContentNegotiator => _innerResult.ContentNegotiator;

        /// <summary>
        /// Gets the request message which led to this result.
        /// </summary>
        public HttpRequestMessage Request => _innerResult.Request;

        /// <summary>
        /// Gets the formatters to use to negotiate and format the content.
        /// </summary>
        public IEnumerable<MediaTypeFormatter> Formatters => _innerResult.Formatters;

        /// <inheritdoc/>
        public virtual async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var result = GetInnerActionResult();
            var response = await result.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }

        internal IHttpActionResult GetInnerActionResult()
        {
            return _innerResult;
        }

        private static T CheckNull(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return entity;
        }
    }
}