namespace accreditation_portal.Services
{
    // Thrown for expected business-rule violations (wrong file type, already submitted, missing
    // documents, etc.) so controllers can catch this specifically and show the message to the user,
    // as opposed to an unexpected exception that should bubble up as a 500.
    public class ApplicationOperationException : Exception
    {
        public ApplicationOperationException(string message) : base(message)
        {
        }
    }
}
