import { OpenInNew } from '@mui/icons-material';
import { Grid, Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import { LoginState } from '../state/Login.state';
import { useRecoilState, useRecoilValue } from 'recoil';
import { useEffect, useState } from 'react';
import { CommonState, CommonStateModel, DataHolderName, DataRecipientName, DataHolderAbn } from '../state/Common.state';
import { OtpForm } from '../components/OtpForm';
import { PageLayout } from '../components/PageLayout';
import { useHistory } from 'react-router-dom';
import { ConsentState } from '../state/Consent.state';
import settings from '../settings';
import { useAuth } from '../hooks/useAuth';
import { LoginForm } from '../components/LoginForm';

export default function Login() {
    const [loginState, setLoginState] = useRecoilState(LoginState);
    const [consentState, setConsentState] = useRecoilState(ConsentState);
    const [commonState, setCommonState] = useRecoilState(CommonState);
    const dataHolderName = useRecoilValue(DataHolderName);
    const dataHolderAbn = useRecoilValue(DataHolderAbn);
    const dataRecipientName = useRecoilValue(DataRecipientName);
    const queryParams = new URLSearchParams(window.location.search); //TODO:C maybe move this into the useEffect
    const cdrPolicyLink = settings.CDR_POLICY_LINK ?? "";
    const [customerIdState, setCustomerIdState] = useState('');
    const [otpState, setOtpState] = useState('');
    const history = useHistory();
    const auth = useAuth();

    useEffect(() => {
        const code = queryParams.get("code");
        if (code === null || code === "") {
            setCommonState((currentValue: CommonStateModel) => {
                return {
                    ...currentValue,
                    errors: [{ title: "Login Failed", code: "No Token", detail: "Authentication Token not provided. Please provide a valid token and re-start the application." }]
                } as CommonStateModel;
            });

            return;
        }

        // Validate the token
        auth.validateToken(code).then(loginParams => {
            if (loginParams == null) {
                // Go to a error page
                setCommonState((currentValue: CommonStateModel) => {
                    return {
                        ...currentValue,
                        errors: [...currentValue.errors ?? [], { title: "Login Failed", code: "Invalid Token", detail: "The token provided is invalid. Please provide a valid token and re-start the application." }]
                    } as CommonStateModel;
                });
                return;
            }

            setCommonState((currentValue: CommonStateModel) => {
                return {
                    ...currentValue,
                    errors: undefined,
                    inputParams: loginParams
                } as CommonStateModel;
            });
            setCustomerIdState(loginParams?.customer_id ?? "");
            setOtpState(loginParams?.otp ?? "");

            setCommonState((currentValue: CommonStateModel) => {
                return {
                    ...currentValue,
                    dataHolder: { BrandId: '', BrandName: loginParams?.dh_brand_name, BrandAbn: loginParams?.dh_brand_abn },
                    dataRecipient: { BrandId: '', BrandName: loginParams?.dr_brand_name },
                } as CommonStateModel;
            });
        });

    }, []);

    const onLoginComplete = () => {
        // Save the consent related data.
        setConsentState({ ...consentState, subjectId: loginState.customerId });

        history.push('/ui/select-accounts');
    }

    return (
        <PageLayout>
            {!loginState || loginState.customerId === "" ? <LoginForm customerId={customerIdState} /> : <OtpForm otp={otpState} onComplete={onLoginComplete} />}
            <Grid container justifyContent="center" mt={4}>
                <Grid item xs={12} bgcolor={grey[100]} p={2}>
                    <Typography color="inherit" variant="body2" pb={2}>
                        We will never share your login details with {dataRecipientName} or ask you to provide your real password to share CDR data.
                    </Typography>
                    <Typography color="inherit" variant="body2" pb={2}>
                        For more information view our <a href={"#"} tabIndex={5}>CDR Policy&nbsp;<OpenInNew sx={{ fontSize: 8 }} /></a> (2 min read)
                    </Typography>
                    <Typography color="inherit" variant="body2">
                        © {dataHolderName} Limited ABN {dataHolderAbn}
                    </Typography>
                </Grid>
            </Grid>
        </PageLayout>
    )
}