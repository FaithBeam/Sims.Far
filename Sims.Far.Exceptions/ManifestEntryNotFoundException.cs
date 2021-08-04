using System;

namespace Sims.Far.Exceptions
{
    public class ManifestEntryNotFoundException : Exception
    {
        public ManifestEntryNotFoundException()
        {
        }

        public ManifestEntryNotFoundException(string message)
            : base(message)
        {
        }

        public ManifestEntryNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}