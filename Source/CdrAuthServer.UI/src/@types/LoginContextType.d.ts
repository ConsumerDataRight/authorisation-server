import { CustomerModel } from "../models/DataModels";
import { LoginInputModel, OtpInputModel } from "../models/LoginModels";

export type LoginState = LoginInputModel & OtpInputModel & {
    customer?: CustomerModel
}

export type LoginContextType = {
    loginState: LoginState,
    setLoginState: (updatedState: LoginState) => void;
}