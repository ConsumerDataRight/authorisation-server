const settings = {
    DATA_FILE_NAME: window.env?.REACT_APP_DATA_FILE_NAME || import.meta.env.REACT_APP_DATA_FILE_NAME,
    CLUSTER_DATA_FILE_NAME: window.env?.REACT_APP_CLUSTER_DATA_FILE_NAME || import.meta.env.REACT_APP_CLUSTER_DATA_FILE_NAME,
    CDR_POLICY_LINK: window.env?.REACT_APP_CDR_POLICY_LINK || import.meta.env.REACT_APP_CDR_POLICY_LINK,
    CDR_FAQ_LINK: window.env?.REACT_APP_CDR_FAQ_LINK || import.meta.env.REACT_APP_CDR_FAQ_LINK,
    DEFAULT_USER_NAME_TEXT: window.env?.REACT_APP_DEFAULT_USER_NAME_TEXT || import.meta.env.REACT_APP_DEFAULT_USER_NAME_TEXT,
    JWKS_URI: window.env?.REACT_APP_JWKS_URI || import.meta.env.REACT_APP_JWKS_URI
};

export default settings;