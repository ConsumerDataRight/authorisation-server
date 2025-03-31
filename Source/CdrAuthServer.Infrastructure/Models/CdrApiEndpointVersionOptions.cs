namespace CdrAuthServer.Infrastructure.Models
{
    public class CdrApiEndpointVersionOptions
    {
        public string Path { get; }

        public bool IsVersioned { get; }

        public bool IsXVHeaderMandatory { get; }

        public int? CurrentMinVersion { get; }

        public int? CurrentMaxVersion { get; }

        public int? MinVerForResponseErrorListV2 { get; }// any supported versions earlier than this number will use ResponseErrorList (v1) as per the CDS standards. Potentially everything uses ResponseErrorListv2 now

        /// <summary>
        /// Initializes a new instance of the <see cref="CdrApiEndpointVersionOptions"/> class.
        /// option set where multiple versions of the endpoint are supported.
        /// </summary>
        /// <param name="path">Path of API endpoints.</param>
        /// <param name="isXvMandatory">Options to specifiy Xv is mandatory.</param>
        /// <param name="minVersion">minVersion of the API.</param>
        /// <param name="maxVersion">maxVersion of the API.</param>
        /// <param name="minVersionForErrorListV2">minVersionForErrorListV2.</param>
        public CdrApiEndpointVersionOptions(string path, bool isXvMandatory, int minVersion, int maxVersion, int minVersionForErrorListV2)
        {
            Path = path;
            IsVersioned = true;
            IsXVHeaderMandatory = isXvMandatory;
            CurrentMinVersion = minVersion;
            CurrentMaxVersion = maxVersion;
            MinVerForResponseErrorListV2 = minVersionForErrorListV2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdrApiEndpointVersionOptions"/> class.
        /// Constructs an option set where only one version of the endpoint is supported.
        /// </summary>
        /// <param name="path">Path of API endpoints.</param>
        /// <param name="isXvMandatory">Options to specifiy Xv is mandatory.</param>
        /// <param name="version">Version of the API.</param>
        public CdrApiEndpointVersionOptions(string path, bool isXvMandatory, int version)
        {
            Path = path;
            IsVersioned = true;
            IsXVHeaderMandatory = isXvMandatory;
            CurrentMinVersion = version;
            CurrentMaxVersion = version;
            MinVerForResponseErrorListV2 = version;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdrApiEndpointVersionOptions"/> class.
        /// Constructs an option set where the endpoint is unversioned.
        /// </summary>
        /// <param name="path"> Path of API endpoints.</param>
        public CdrApiEndpointVersionOptions(string path)
        {
            Path = path;
            IsVersioned = false;
            IsXVHeaderMandatory = false;
        }
    }
}
