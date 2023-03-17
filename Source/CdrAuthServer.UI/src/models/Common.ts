import { JWK } from "jwk-to-pem";

export type Nullable<T> = T | null;

export enum AlertTypeEnum {
    Success = 1,
    Error = 2,
    Sms = 3
}

export type AlertModel = {
    isOpen: boolean,
    message?: string,
    title?: string,
    subTitle?: string,
    errorList?: ErrorListModel,
    type?: AlertTypeEnum
}

export type ErrorListModel = {
    errors: ErrorModel[]
}

export type ErrorModel = {
    code: string,
    title: string,
    detail: string
}

export type JwkKey = JWK & {kid?:string}

export type JwksResponse = {
    keys:JwkKey[]
}
