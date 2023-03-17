import { atom } from "recoil";

export type ConsentStateModel = {
    subjectId?: string,
    accountIds?: string[],
    errorCode?: string,
}

export const ACCESS_DENIED_ERROR_CODE = "ERR-AUTH-009";

export const ConsentState = atom<ConsentStateModel>({
    key: 'ConsentState',
    default: { }
});