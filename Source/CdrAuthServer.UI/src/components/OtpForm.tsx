import { yupResolver } from "@hookform/resolvers/yup";
import { Typography, Grid, TextField, Button, Link } from "@mui/material";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useRecoilValue, useRecoilState } from "recoil";
import { useAlert } from "../hooks/useAlert";
import { AlertTypeEnum } from "../models/Common";
import { OtpInputModel } from "../models/LoginModels";
import { CommonState, DataHolderName } from "../state/Common.state";
import { LoginState } from "../state/Login.state";
import * as Yup from 'yup';
import { useData } from "../hooks/useData";

export function OtpForm({ otp, onComplete }: { otp: string, onComplete: () => void }) {
    const { createAlert, closeAlert } = useAlert();
    const dataHolderName = useRecoilValue(DataHolderName);
    const [loginState, setLoginState] = useRecoilState(LoginState);
    const [commonState, _] = useRecoilState(CommonState);
    const { submitCancelConsentRequest } = useData();

    useEffect(() => {
        showOtp();
    }, []);

    const showOtp = () => {
        setTimeout(() => {
            createAlert(`From ${dataHolderName}: 000789 is your One Time Password to share your CDR data.`, AlertTypeEnum.Sms, `${dataHolderName}`);
            setSecondsLeft(600);
        }, 1000);
    }

    // Timer functionality
    const [secondsLeft, setSecondsLeft] = useState(600);
    const displayTimeLeft = (seconds: number) => {
        const minutesLeft = Math.floor(seconds / 60);
        const secondsLeft = seconds % 60;
        const minutesLeftStr = minutesLeft.toString().length === 1 ? "0" + minutesLeft : minutesLeft;
        const secondsLeftStr = secondsLeft.toString().length === 1 ? "0" + secondsLeft : secondsLeft;
        return `${minutesLeftStr}:${secondsLeftStr}`;
    }
    useEffect(() => {
        if (secondsLeft === 0) return;

        const intervalId = setInterval(() => {
            setSecondsLeft(secondsLeft - 1);
        }, 1000);

        return () => clearInterval(intervalId);
    }, [secondsLeft]);


    const defaultFormValues = {
        otp: otp,
    };
    const validationSchema = Yup.object().shape({
        otp: Yup.string().required('One Time Password is required'),
    });
    const {
        setError,
        control,
        handleSubmit,
        formState: { errors },
    } = useForm<OtpInputModel>({
        resolver: yupResolver(validationSchema),
        defaultValues: defaultFormValues,
        shouldUnregister: false
    });

    const onSubmit = (data: OtpInputModel) => {
        if (data.otp !== "000789") {
            setLoginState({ ...loginState, otp: '' });
            setError('otp', { type: 'custom', message: 'Invalid One Time Password' });
            return;
        }

        setLoginState({ ...loginState, otp: data.otp });
        closeAlert();
        onComplete();
    }

    const cancelRequest = () => {
        submitCancelConsentRequest();
    }

    return (
        <>
            <Typography color="inherit" variant="h5" pb={2}>
                One Time Password
            </Typography>
            <Typography color="inherit" variant="body1" pb={1}>
                Enter the code sent to &#x2022;&#x2022;&#x2022;&#x2022; &#x2022;&#x2022;&#x2022; &#x2022;&#x2022;&#x2022; 190.
            </Typography>
            <Typography color="inherit" variant="body1" pb={2}>
                This code will expire in <b>{displayTimeLeft(secondsLeft)}</b>.
            </Typography>
            <form onSubmit={handleSubmit(onSubmit)}>
                <Grid container rowSpacing={2} mt={1} mb={2}>
                    <Grid item xs={12}>
                        <Controller
                            name="otp"
                            control={control}
                            rules={{ required: true }}
                            render={({ field: { onChange, value }, formState: { errors } }) => <TextField
                                label="One Time Password - Required"
                                fullWidth
                                value={value}
                                onChange={onChange}
                                error={errors.otp ? true : false}
                                helperText={errors.otp?.message}
                                inputProps={{ tabIndex: 1 }}
                            />}
                        />
                    </Grid>
                    <Grid item xs={12}>
                        <Typography color="inherit" variant="body2">
                            Didn't get the code?&nbsp;<Link tabIndex={4} onClick={showOtp}>Resend code</Link>&nbsp;or contact {dataHolderName} to update your details.
                        </Typography>
                    </Grid>
                    <Grid item xs={12}>
                        <Grid container spacing={2}>
                            <Grid item xs={6}>
                                <Button
                                    variant="outlined"
                                    color="primary"
                                    fullWidth
                                    size={'large'}
                                    tabIndex={2}
                                    aria-label="cancel"
                                    onClick={cancelRequest}
                                >
                                    Cancel
                                </Button>
                            </Grid>
                            <Grid item xs={6}>
                                <Button
                                    type='submit'
                                    variant="contained"
                                    color="primary"
                                    fullWidth
                                    size={'large'}
                                    tabIndex={3}
                                    aria-label="continue"
                                >
                                    Continue
                                </Button>
                            </Grid>
                        </Grid>
                    </Grid>
                </Grid>
            </form>
        </>
    );
}