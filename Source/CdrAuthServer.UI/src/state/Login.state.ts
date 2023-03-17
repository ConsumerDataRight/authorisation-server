import { atom } from "recoil";
import { CustomerModel } from "../models/DataModels";
import { LoginInputModel, OtpInputModel } from "../models/LoginModels";

export type LoginStateModel = LoginInputModel & OtpInputModel & {
    customer?: CustomerModel
}

export const LoginState = atom<LoginStateModel>({
    key: 'LoginState',
    default: { customerId: '', otp: ''}
});