import { Container, Grid, Typography, Alert, AlertTitle } from "@mui/material";
import { useCommonContext } from "../context/CommonContext";

export function PageLayout({ children }: { children: any }) {
    const {commonState} = useCommonContext();
    const dataHolderName = commonState.dataHolder?.BrandName;

    return (
        <Container maxWidth={'xs'}>
            <Grid container justifyContent="center" px={1} py={3} mt={3}>
                <Grid item xs={12}>
                    {commonState?.errors && commonState.errors.map((e, index) => (
                        <Alert key={`error-${index}`} severity="error" sx={{ mb: 3 }}>
                            <AlertTitle>{e.title}</AlertTitle>
                            <b>{e.code}</b>&nbsp;-&nbsp;{e.detail}
                        </Alert>
                    ))}
                    {!commonState?.errors && <>
                        <img src={process.env.PUBLIC_URL + '/cdr-logo.png'} />
                        <Typography color="inherit" variant="h6" pb={2}>
                            {dataHolderName}
                        </Typography>
                        {children}
                    </>}
                </Grid>
            </Grid>
        </Container>
    );
}
