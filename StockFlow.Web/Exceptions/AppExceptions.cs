using System.Net;

namespace StockFlow.Web.Exceptions
{
    public class AppException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? Details { get; }

        public AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = ErrorMessages.Auth.Unauthorized)
            : base(message, HttpStatusCode.Unauthorized) { }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = ErrorMessages.Auth.InsufficientRole)
            : base(message, HttpStatusCode.Forbidden) { }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message = ErrorMessages.General.NotFound)
            : base(message, HttpStatusCode.NotFound) { }
    }

    public class ValidationException : AppException
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(string message, IEnumerable<string>? errors = null)
            : base(message, HttpStatusCode.UnprocessableEntity)
        {
            Errors = errors ?? Enumerable.Empty<string>();
        }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, HttpStatusCode.Conflict) { }
    }

    public class WeightValidationException : AppException
    {
        public double ParentWeight { get; }
        public double ChildrenTotalWeight { get; }

        public WeightValidationException(double parentWeight, double childrenTotalWeight)
            : base(ErrorMessages.Process.WeightExceedsParent, HttpStatusCode.UnprocessableEntity)
        {
            ParentWeight = parentWeight;
            ChildrenTotalWeight = childrenTotalWeight;
        }
    }

    public class CircularReferenceException : AppException
    {
        public CircularReferenceException()
            : base(ErrorMessages.Process.CircularReference, HttpStatusCode.UnprocessableEntity) { }
    }

    public class ExportException : AppException
    {
        public ExportException(string message = ErrorMessages.Export.GenerationFailed)
            : base(message, HttpStatusCode.InternalServerError) { }
    }

    public class DatabaseException : AppException
    {
        public DatabaseException(string message = ErrorMessages.General.DatabaseError)
            : base(message, HttpStatusCode.InternalServerError) { }
    }

    public class ServiceUnavailableException : AppException
    {
        public ServiceUnavailableException(string message = ErrorMessages.General.ServiceUnavailable)
            : base(message, HttpStatusCode.ServiceUnavailable) { }
    }
}