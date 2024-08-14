import {createContext, useContext} from "react";
import {CommonContextType} from "../@types/CommonContextType";

export const CommonContext = createContext<CommonContextType>({
    commonState: {pageTitle:""},
    setCommonState: () => {}
});

export const useCommonContext = () => useContext<CommonContextType>(CommonContext);