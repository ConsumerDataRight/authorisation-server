import {createContext, useContext} from "react";
import {LoginContextType} from "../@types/LoginContextType";

export const LoginContext = createContext<LoginContextType>({
    loginState: {customerId:"",otp:""},
    setLoginState: () => {}
});

export const useLoginContext = () => useContext(LoginContext);