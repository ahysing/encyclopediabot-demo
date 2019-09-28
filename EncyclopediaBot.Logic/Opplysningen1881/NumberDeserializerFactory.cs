using System;

namespace EncyclopediaBot.Logic.Opplysningen1881
{
    internal class NumberDeserializerFactory
    {
        public virtual NumberDeserializer Create(string url, Guid? requestId, ILogger logger)
        {
            return new NumberDeserializer(url, requestId, logger);
        }
    }
}