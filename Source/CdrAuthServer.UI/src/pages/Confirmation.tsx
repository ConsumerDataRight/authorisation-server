
import { OpenInNew } from "@mui/icons-material";
import { Box, Button, Grid, Link, Typography } from "@mui/material";
import { grey } from "@mui/material/colors";
import { useEffect, useState } from "react";
import ClusterList from "../components/ClusterList";
import { PageLayout } from "../components/PageLayout";
import { useData } from "../hooks/useData";
import settings from "../settings";
import { useCommonContext } from "../context/CommonContext";

export default function Confirmation() {
    const { commonState } = useCommonContext();
    const dataRecipientName = commonState.dataRecipient?.BrandName;
    const dataHolderName = commonState.dataHolder?.BrandName;
    const sharingDuration = commonState.inputParams?.sharing_duration!;
    const cdrPolicyLink = settings.CDR_POLICY_LINK ?? "";
    const [consentDays, setConsentDays] = useState<number>(0);
    const [isConsentOneoff, setIsConsentOneOff] = useState<boolean>(false);
    const { submitConsentRequest, submitCancelConsentRequest } = useData();

    // Cluster functionality
    const [openedClusterIds, setOpenedClusterIds] = useState<string[]>([]);
    const toggleClusters = (clusterId: string) => () => {
        var clusterIdClone = [...openedClusterIds];
        const existingIndex = openedClusterIds.indexOf(clusterId);
        if (existingIndex > -1) {
            clusterIdClone.splice(existingIndex, 1);
        }
        else {
            clusterIdClone.push(clusterId);
        }
        setOpenedClusterIds(clusterIdClone);
    }

    useEffect(() => {
        if (sharingDuration >= 86400) {
            setConsentDays(Math.floor(sharingDuration / 86400));
        }
        if (sharingDuration / 86400 <= 1) {
            setIsConsentOneOff(true);
        }

    }, [sharingDuration])

    const onSubmit = () => {
        submitConsentRequest();
    }

    const cancelRequest = () => {
        submitCancelConsentRequest();
    }

    const SharingClusterMoreInformation = () => {
        var sharingInformation = "We will share your data once.";
        if (isConsentOneoff === false) {
            sharingInformation = `We will share your data on an ongoing basis for the next ${(consentDays >= 365 ? "12 months" : consentDays + " day(s)")}.`;
        }

        return (
        <>
            <Typography pt={2} pb={2}>{sharingInformation}</Typography>
            {consentDays > 365 && <Typography pt={2} pb={2} sx={{ fontStyle: 'italic' }}>* Sharing Period is a maximum of 12 months</Typography>}
        </>);
    }

    return (
        <PageLayout>
            <Typography color="inherit" variant="h5" pb={2}>
                Confirm what we'll share
            </Typography>
            <Typography color="inherit" variant="body2" pb={2}>
                Please confirm that you agree to share the following data with {dataRecipientName}.
            </Typography>
            <Typography color="inherit" variant="h6" pt={2} pb={1}>
                Data requested
            </Typography>
            <ClusterList scopes={commonState.inputParams?.scope ?? ""} />

            <Typography color="inherit" variant="h6" mt={4} mb={1}>
                Sharing period
            </Typography>
            <Box mb={3} sx={{ p: 2, borderTop: `1px solid ${grey[300]}`, borderBottom: `1px solid ${grey[300]}` }}>
                {isConsentOneoff && <Typography pb={1}>Once</Typography>}

                {!isConsentOneoff && <Typography pb={1}>[{new Date().toLocaleString('en-au', { day: 'numeric', month: 'long', year: 'numeric' })} -
                    {new Date(new Date().getTime() + (1000 * sharingDuration)).toLocaleString('en-au', { day: 'numeric', month: 'long', year: 'numeric' })}]</Typography>}

                {openedClusterIds.includes("sharing.period") && <SharingClusterMoreInformation />}
                <Link underline="always" onClick={toggleClusters("sharing.period")}>See {openedClusterIds.includes("sharing.period") ? "less" : "more"}</Link>
            </Box>

            <Typography color="inherit" variant="h6" mt={4} mb={1}>
                Manage your data sharing
            </Typography>
            <Box mb={3} sx={{ p: 2, borderTop: `1px solid ${grey[300]}`, borderBottom: `1px solid ${grey[300]}` }}>
                <Typography pb={1}>Go to 'Settings&gt;Data sharing' to review this arrangement and stop sharing your data.</Typography>
                {openedClusterIds.includes("sharing.manage") && <><Typography pt={2} pb={2}>
                    You can do this at any time in online banking or on the {dataHolderName} app.
                </Typography>
                    <Typography>
                        You can also tell us to stop sharing your data by writing to mail@example.com.au
                    </Typography></>}
                <Link underline="always" onClick={toggleClusters("sharing.manage")}>See {openedClusterIds.includes("sharing.manage") ? "less" : "more"}</Link>
            </Box>

            <Typography color="inherit" variant="body2" mt={4}>
                <a href={"#"} tabIndex={5}>View our CDR policy (2 min read) &nbsp;<OpenInNew sx={{ fontSize: 8 }} /></a>
            </Typography>

            <Grid container mt={4} justifyContent="center" bgcolor={grey[100]}>
                <Grid item xs={12} p={2}>
                    <Typography color="inherit" variant="h6">
                        Do you allow us to share your data with {dataRecipientName}?
                    </Typography>
                </Grid>
                <Grid item xs={12} p={2}>
                    <Grid container spacing={2}>
                        <Grid item xs={6}>
                            <Button
                                variant="contained"
                                color="primary"
                                fullWidth
                                size={'large'}
                                tabIndex={2}
                                aria-label="cancel"
                                onClick={cancelRequest}
                            >
                                Deny
                            </Button>
                        </Grid>
                        <Grid item xs={6}>
                            <Button
                                type='submit'
                                variant="contained"
                                color="primary"
                                fullWidth
                                size={'large'}
                                onClick={onSubmit}
                                tabIndex={3}
                                aria-label="continue"
                            >
                                Authorise
                            </Button>
                        </Grid>
                    </Grid>
                </Grid>
            </Grid>
        </PageLayout>
    );
}