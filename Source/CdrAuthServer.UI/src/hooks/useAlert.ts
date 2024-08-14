import { AlertTypeEnum, ErrorListModel } from "../models/Common";
import { useCommonContext } from "../context/CommonContext";

export function useAlert() {
    const {commonState, setCommonState} = useCommonContext();

    const createAlert = (message: string, type: AlertTypeEnum = AlertTypeEnum.Success, title: string | undefined = undefined, subTitle: string | undefined = undefined): void => {
        setCommonState({ ...commonState, alert: { isOpen: true, message: message, type: type, title: title, subTitle: subTitle } });
    };

    const createErrorListAlert = (errorList: ErrorListModel, message: string): void => {
        setCommonState({ ...commonState, alert: { isOpen: true, message: message, errorList: errorList, type: AlertTypeEnum.Error } });
    };

    const closeAlert = (): void => {
        setCommonState({ ...commonState, alert: { isOpen: false } });
    }

    return {
        createAlert,
        closeAlert,
        createErrorListAlert,
        alert: commonState.alert
    };
};