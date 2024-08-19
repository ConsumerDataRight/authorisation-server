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

export type CommonState = {
    pageTitle: string,
    alert?: AlertModel,
    dataHolder?: BrandModel,
    dataRecipient?: BrandModel,
    inputParams?: LoginParams,
    errors?: ErrorModel[]
}

export type CommonContextType = {
    commonState: CommonState;
    setCommonState: (updatedState: CommonState) => void;
}