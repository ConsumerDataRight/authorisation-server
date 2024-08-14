export type ConsentState = {
    subjectId?: string,
    accountIds?: string[],
    errorCode?: string,
}

export type ConsentContextType = {
    consentState: ConsentState,
    setConsentState: (updatedState: ConsentState) => void;
}

export const ACCESS_DENIED_ERROR_CODE = "ERR-AUTH-009";
