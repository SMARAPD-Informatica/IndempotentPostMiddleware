using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IndempotentPost
{
    public class IndempotentPostMiddleware
    {
        private readonly RequestDelegate _next;
        public IndempotentPostMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IMemoryCache cache)
        {
            var url = context.Request.Path;
            var user = context.User.Identity.Name;
            string cacheKey = $"{user}_{url}";
            if (context.Request.Method == HttpMethods.Post)
            {
                context.Request.EnableBuffering();
                var request = context.Request;
                var stream = new StreamReader(request.Body);
                var body = await stream.ReadToEndAsync();
                context.Request.Body.Position = 0;

                var hash = body.GetHashCode();

                if (cache.TryGetValue(cacheKey, out int value) && value == hash)
                {
                    throw new Exception("Requisição negada.");
                }
                else
                {
                    cache.Set(cacheKey, hash, TimeSpan.FromMinutes(2));
                }
            }

            await _next.Invoke(context);

            if (cache.TryGetValue(cacheKey, out _))
            {
                cache.Remove(cacheKey);
            }

        }
    }

}
