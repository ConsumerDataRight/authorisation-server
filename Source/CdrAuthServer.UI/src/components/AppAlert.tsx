import { Sms } from '@mui/icons-material';
import { Snackbar, Alert, AlertTitle, Typography } from '@mui/material';
import { useAlert } from '../hooks/useAlert';
import { AlertTypeEnum } from "../models/Common";

export function AppAlert() {
    const { closeAlert, alert } = useAlert();

    const renderMessage = () => {
        switch (alert?.type) {
            case AlertTypeEnum.Error:
                return (
                    <Alert severity="error" sx={{ width: '100%' }} onClose={closeAlert}>
                        <AlertTitle>{alert.message}</AlertTitle>
                        {alert.errorList ? renderErrorList() : '(No additional information)'}
                    </Alert>
                );

            case AlertTypeEnum.Sms:
                return (
                    <Alert icon={<Sms />} severity="error" color='warning' sx={{ width: '100%' }} onClose={closeAlert}>
                        <AlertTitle>NEW MESSAGE</AlertTitle>
                        <Typography color="inherit" variant="body2" pb={2}>
                            <b>{alert?.title && <span>{alert?.title}<br /></span>}</b>
                            {alert.message}
                        </Typography>
                    </Alert>
                );

            default:
                return (
                    <Alert severity="success" sx={{ width: '100%' }}>
                        {alert?.message}
                    </Alert>
                );
        }
    }

    const renderErrorList = () => (
        <>
            <ul>
                {alert?.errorList?.errors.map((error: any, index: any) => (
                    <li key={`alert-${index}`}>{error.detail}</li>
                ))}
            </ul>
        </>
    )

    return (
        <>
            {alert?.isOpen === true && <Snackbar open={alert?.isOpen} autoHideDuration={alert?.type === AlertTypeEnum.Error || alert?.type === AlertTypeEnum.Sms ? null : 6000} onClose={alert?.type === AlertTypeEnum.Error || alert?.type === AlertTypeEnum.Sms ? undefined : closeAlert} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
                {renderMessage()}
            </Snackbar>}
        </>
    );
}