namespace StockFlow.Web.Exceptions
{
    public static class ErrorMessages
    {
        public static class Auth
        {
            public const string InvalidCredentials = "Invalid email or password.";
            public const string AccountNotFound = "No account found with this email.";
            public const string TokenExpired = "Your session has expired. Please log in again.";
            public const string TokenInvalid = "Invalid authentication token.";
            public const string Unauthorized = "You are not authorized to perform this action.";
            public const string InsufficientRole = "Your role does not have permission for this operation.";
            public const string PasswordTooWeak = "Password must be at least 8 characters with one number and one special character.";
        }

        public static class Item
        {
            public const string NotFound = "Item not found.";
            public const string DuplicateSKU = "An item with this SKU already exists.";
            public const string CannotDelete = "This item is linked to existing shipments and cannot be deleted.";
            public const string InvalidUnit = "Provided unit of measurement is not valid.";
        }

        public static class Shipment
        {
            public const string NotFound = "Shipment not found.";
            public const string AlreadyProcessed = "This shipment has already been fully processed.";
            public const string InvalidWeight = "Shipment weight must be greater than zero.";
            public const string CannotDelete = "Processed shipments cannot be deleted.";
        }

        public static class Process
        {
            public const string NotFound = "Processed item not found.";
            public const string WeightExceedsParent = "Total child weight cannot exceed parent item weight.";
            public const string NoChildren = "At least one child item is required to process.";
            public const string AlreadyApproved = "This item has already been approved and cannot be modified.";
            public const string AlreadyRejected = "This item has already been rejected.";
            public const string CannotApproveOwn = "Managers cannot approve their own processed items.";
            public const string ParentNotFound = "Parent item not found in the processing tree.";
            public const string CircularReference = "A circular reference was detected in the item tree.";
            public const string InvalidDepth = "Maximum processing tree depth exceeded.";
        }

        public static class Audit
        {
            public const string LogFailed = "Audit log could not be written.";
        }

        public static class Export
        {
            public const string GenerationFailed = "Export file could not be generated. Please try again.";
            public const string UnsupportedFormat = "Requested export format is not supported.";
        }

        public static class Search
        {
            public const string QueryTooShort = "Search query must be at least 2 characters.";
            public const string NoResults = "No results found for your search.";
        }

        public static class General
        {
            public const string ServerError = "An unexpected error occurred. Please try again later.";
            public const string DatabaseError = "A database error occurred. Please contact support.";
            public const string NotFound = "The requested resource was not found.";
            public const string BadRequest = "The request was invalid or malformed.";
            public const string ServiceUnavailable = "The service is temporarily unavailable.";
        }
    }
}