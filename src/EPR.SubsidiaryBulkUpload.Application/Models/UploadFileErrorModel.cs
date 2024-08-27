﻿namespace EPR.SubsidiaryBulkUpload.Application.Models;

public class UploadFileErrorModel
{
    public int FileLineNumber { get; set; }

    public string FileContent { get; set; }

    public string Message { get; set; }

    public bool IsError { get; set; }
}
