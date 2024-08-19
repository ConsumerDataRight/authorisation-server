import { OpenInNew } from '@mui/icons-material';
import { Grid, Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import { useEffect, useState } from 'react';
import { OtpForm } from '../components/OtpForm';
import { PageLayout } from '../components/PageLayout';
import { useNavigate } from 'react-router-dom';
import settings from '../settings';
import { useAuth } from '../hooks/useAuth';
import { LoginForm } from '../components/LoginForm';
import { useCommonContext } from '../context/CommonContext';
import { useLoginContext } from '../context/LoginContext';
import { useConsentContext } from '../context/ConsentContext';

export default function Login() {
    const { loginState } = useLoginContext();
    const { commonState, setCommonState } = useCommonContext();
    const { consentState, setConsentState } = useConsentContext();
    const queryParams = new URLSearchParams(window.location.search); //TODO:C maybe move this into the useEffect
    const cdrPolicyLink = settings.CDR_POLICY_LINK ?? "";
    const [customerIdState, setCustomerIdState] = useState('');
    const [otpState, setOtpState] = useState('');
    const navigate = useNavigate();
    const auth = useAuth();

    useEffect(() => {
        const code = queryParams.get("code");
        if (code === null || code === "") {
            setCommonState({
                ...commonState,
                errors: [{ title: "Login Failed", code: "No Token", detail: "Authentication Token not provided. Please provide a valid token and re-start the application." }]
            });
            return;
        }

        // Validate the token
        auth.validateToken(code).then(loginParams => {
            if (loginParams == null) {
                // Go to a error page
                setCommonState({
                    ...commonState,
                    errors: [...commonState.errors ?? [], { title: "Login Failed", code: "Invalid Token", detail: "The token provided is invalid. Please provide a valid token and re-start the application." }]
                });
                return;
            }

            setCustomerIdState(loginParams?.customer_id ?? "");
            setOtpState(loginParams?.otp ?? "");

            setCommonState({
                ...commonState,
                errors: undefined,
                inputParams: loginParams,
                dataHolder: { BrandId: '', BrandName: loginParams?.dh_brand_name ?? "[Brand Name]", BrandAbn: loginParams?.dh_brand_abn },
                dataRecipient: { BrandId: '', BrandName: loginParams?.dr_brand_name ?? "[Brand Name]" },
            });
        });

    }, []);

    const onLoginComplete = () => {
        // Save the consent related data.
        setConsentState({ ...consentState, subjectId: loginState.customerId });

        navigate('/ui/select-accounts');
    }

    return (
        <PageLayout>
            {!loginState || loginState.customerId === "" ? <LoginForm customerId={customerIdState} /> : <OtpForm otp={otpState} onComplete={onLoginComplete} />}
            <Grid container justifyContent="center" mt={4}>
                <Grid item xs={12} bgcolor={grey[100]} p={2}>
                    <Typography color="inherit" variant="body2" pb={2}>
                        We will never share your login details with {commonState?.dataRecipient?.BrandName} or ask you to provide your real password to share CDR data.
                    </Typography>
                    <Typography color="inherit" variant="body2" pb={2}>
                        For more information view our <a href={"#"} tabIndex={5}>CDR Policy&nbsp;<OpenInNew sx={{ fontSize: 8 }} /></a> (2 min read)
                    </Typography>
                    <Typography color="inherit" variant="body2">
                        © {commonState?.dataHolder?.BrandName} Limited ABN {commonState?.dataHolder?.BrandAbn}
                    </Typography>
                </Grid>
            </Grid>
        </PageLayout>
    )
}