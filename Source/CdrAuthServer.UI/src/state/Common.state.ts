import { atom, selector } from "recoil";
import { AlertModel, ErrorModel } from "../models/Common";
import { BrandModel } from "../models/DataModels";

export type AuthorizeRequest = {
    request_uri: string,
    response_type: string,
    response_mode: string
    client_id: string
    redirect_uri: string
    scope: string
    nonce: string
}

export type LoginParams = {
    return_url?: string,
    dh_brand_name?: string,
    dh_brand_abn?: string,
    dr_brand_name?: string,
    customer_id?: string,
    otp?: string,
    scope?:string,
    authorize_request?: AuthorizeRequest,
    sharing_duration?: number
}

export type CommonStateModel = {
    pageTitle: string,
    alert?: AlertModel,
    dataHolder?: BrandModel,
    dataRecipient?: BrandModel,
    inputParams?: LoginParams,
    errors?: ErrorModel[]
}

export const CommonState = atom<CommonStateModel>({
    key: 'CommonState',
    default: { pageTitle: '', alert: { isOpen: false } }
});

export const DataHolderName = selector({
    key: 'DataHolderName',
    get: ({ get }) => get(CommonState).dataHolder?.BrandName ?? "[Data Holder]",
});
export const DataHolderAbn = selector({
    key: 'DataHolderAbn',
    get: ({ get }) => get(CommonState).dataHolder?.BrandAbn ?? "[ABN]",
});
export const DataRecipientName = selector({
    key: 'DataRecipientName',
    get: ({ get }) => get(CommonState).dataRecipient?.BrandName ?? "[ADR]",
});
export const SharingDuration = selector({
    key: 'SharingDuration',
    get: ({ get }) => get(CommonState).inputParams?.sharing_duration ?? 0
})