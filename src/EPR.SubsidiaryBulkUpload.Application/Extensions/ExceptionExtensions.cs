using System.Text;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class ExceptionExtensions
{
    public static string GetAllMessages(this Exception exception)
    {
        var sb = new StringBuilder();
        var currentException = exception;

        while (currentException != null)
        {
            if (!string.IsNullOrEmpty(currentException.Message))
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(currentException.Message);
            }

            currentException = currentException.InnerException;
        }

        return sb.ToString();
    }
}