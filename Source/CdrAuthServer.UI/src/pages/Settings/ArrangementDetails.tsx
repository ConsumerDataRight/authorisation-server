import { CheckCircle, ChevronRight, Close } from "@mui/icons-material";
import { Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Grid, IconButton, Link, List, ListItem, ListItemButton, ListItemText, Stack, Typography } from "@mui/material";
import { grey } from "@mui/material/colors";
import { InternalLayout } from "../../components/InternalLayout";
import { useHistory, Link as RouterLink } from "react-router-dom";
import { AccountInfo } from "../../components/AccountInfo";
import { useState } from "react";

export default function ArrangementDetails() {
    const history = useHistory();
    const [stopSharingOpen, setStopSharingOpen] = useState(false);

    const gotoAccountDetails = () => {
        history.push('/ui/account-list');
    }

    const onStopSharing = () => {

    }

    return (
        <InternalLayout selectedMenu="settings" pageTitle="[ADR Brand]">
            <Card variant="outlined">
                <CardContent>
                    <Stack
                        direction="row"
                        justifyContent="space-between"
                        alignItems="center"
                        spacing={2}
                    >
                        <Typography color="inherit" variant="h5" pb={1}>
                            [ADR Brand]
                        </Typography>
                        <Chip icon={<CheckCircle />} label="Active" variant="outlined" color="success" size="small" />
                    </Stack>

                    <Typography color="inherit" variant="body1" pb={2}>
                        [software product]
                    </Typography>
                    <Typography color="inherit" variant="body2">
                        Access granted on [01 June 2020]
                    </Typography>
                    <Typography color={'text.secondary'} variant="body2">
                        Expires on [31 May 2021]
                    </Typography>

                </CardContent>
            </Card>
            <Box my={3}>
                <Typography color={'text.secondary'} variant="body2">
                    You should check with the [ADR Brand] app or website for more information on how they are handling your data.
                </Typography>

            </Box>

            <Box my={3}>
                <Typography color={'text.secondary'} variant="button">
                    SHARING ARRANGEMENTS
                </Typography>
                <List
                    sx={{ width: '100%', mt: 1, borderTop: `1px solid ${grey[300]}` }}
                >
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information">
                                <ChevronRight />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} component={RouterLink} to={'/account-list'}>
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Accounts</Typography>} secondary={'Sharing from 3 accounts'} />
                        </ListItemButton>
                    </ListItem>
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information">
                                <ChevronRight />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} component={RouterLink} to={'/data-requested'}>
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Data requseted</Typography>} secondary={'Sharing 3 datasets'} />
                        </ListItemButton>
                    </ListItem>
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information">
                                <ChevronRight />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} component={RouterLink} to={'/sharing-period'}>
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Sharing period</Typography>} secondary={'Sharing for 24 hours'} />
                        </ListItemButton>
                    </ListItem>
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information">
                                <ChevronRight />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} >
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Consent history</Typography>} secondary={'1 CDR receipt'} />
                        </ListItemButton>
                    </ListItem>
                    {/* <Divider component="li" /> */}
                </List>
            </Box>

            <Box my={3} sx={{ p: 2, borderTop: `1px solid ${grey[300]}`, borderBottom: `1px solid ${grey[300]}` }}>
                <Link underline="always">Send this sharing arrangement to your email</Link>
            </Box>

            <Box my={3}>
                <Button variant="outlined" fullWidth size="large" sx={{ py: 1 }} onClick={() => setStopSharingOpen(true)} >Stop sharing</Button>
                <Typography color={'text.secondary'} variant="body2" my={2}>
                    This means [ADR brand] will no londer have access your your data.
                </Typography>
            </Box>

            <Dialog open={stopSharingOpen} maxWidth='xs' scroll="paper">

                <DialogActions sx={{ p: 0 }}>
                    <Button autoFocus onClick={() => setStopSharingOpen(false)} startIcon={<Close />} variant="contained" sx={{ borderRadius: 0 }}>
                        Close
                    </Button>
                </DialogActions>
                <DialogTitle>Stop sharing</DialogTitle>
                <DialogContent>
                    <Typography variant="body1" my={1}>
                        This means [ADR brand] will no londer have access your your data. this may affect your current services to you may want to check with your [ADR brand] before continuing.
                    </Typography>
                    <Typography variant="body1" mt={2} mb={4}>
                        Are you sure you want to continue?
                    </Typography>
                    <Grid container spacing={2}>
                        <Grid item xs={6}>
                            <Button
                                variant="outlined"
                                color="primary"
                                fullWidth
                                size={'large'}
                                tabIndex={2}
                                aria-label="cancel"
                                onClick={() => setStopSharingOpen(false)}
                            >
                                Cancel
                            </Button>
                        </Grid>
                        <Grid item xs={6}>
                            <Button
                                variant="outlined"
                                color="primary"
                                fullWidth
                                size={'large'}
                                onClick={onStopSharing}
                                tabIndex={3}
                                aria-label="continue"
                            >
                                Continue
                            </Button>
                        </Grid>
                    </Grid>
                </DialogContent>
            </Dialog>
        </InternalLayout >
    )

}

{/* TODO: body2 font size should be a bit smaller, override dialog to show at the bottom */ }