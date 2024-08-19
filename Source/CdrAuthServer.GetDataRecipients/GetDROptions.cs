namespace CdrAuthServer.GetDataRecipients
{
    public class GetDROptions
    {
        public string AzureWebJobsStorage { get; set; }
        public string StorageConnectionString { get; set; }
        public string FUNCTIONS_WORKER_RUNTIME {  get; set; }
        public string Register_CdrAuthServer_Logging_DB_ConnectionString { get; set; }
        public string Register_CdrAuthServer_MetadataUpdate_Endpoint { get; set; }
        public string Register_CdrAuthServer_Token_Endpoint { get; set; }
        public string Register_CdrAuthServer_MetadataUpdate_XV { get; set; }
        public string Register_CdrAuthServer_MetadataUpdate_XMINV { get; set; }
        public string Register_Client_Id { get; set; }
        public string Register_Client_Certificate { get; set; }
        public string Register_Client_Certificate_Password { get; set; }
        public string Register_Signing_Certificate { get; set; }
        public string Register_Signing_Certificate_Password { get; set; }
        public string Ignore_Server_Certificate_Errors { get; set; }
    }
}
