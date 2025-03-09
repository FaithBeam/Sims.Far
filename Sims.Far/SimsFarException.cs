using System;

namespace Sims.Far;

public class SimsFarException : Exception
{
    public SimsFarException() { }

    public SimsFarException(string message)
        : base(message) { }

    public SimsFarException(string message, Exception innerException)
        : base(message, innerException) { }
}
