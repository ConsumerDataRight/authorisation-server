import {createContext, useContext} from "react";
import {ConsentContextType} from "../@types/ConsentContextType";

export const ConsentContext = createContext<ConsentContextType>({
    consentState: {},
    setConsentState: () => {}
});

export const useConsentContext = () => useContext(ConsentContext);