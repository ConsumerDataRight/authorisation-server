import { Close, MoreHoriz } from "@mui/icons-material";
import { Box, List, ListItem, IconButton, ListItemButton, ListItemText, Typography, Dialog, Button, DialogActions, DialogContent } from "@mui/material";
import { grey } from "@mui/material/colors";
import { useState } from "react";
import { AccountInfo } from "../../components/AccountInfo";
import { InternalLayout } from "../../components/InternalLayout";
import { AccountModel } from "../../models/DataModels";

export default function AccountList() {
    const sampleAccount: AccountModel = { AccountId: '10-1-1', DisplayName: 'Saving Account', ProductName: 'Everyday Savings Account', MaskedName: 'xxx-xxx xxxxx455' };
    const [accountDetails, setAccountDetails] = useState<{ open: boolean, account?: AccountModel }>({ open: true });

    const showAccountDetails = (account: AccountModel) => () => {
        setAccountDetails({ open: true, account: account });
    }

    const onClose = () => {
        setAccountDetails({ open: false, account: undefined });
    }

    return (
        <InternalLayout selectedMenu="settings" pageTitle="Accounts">
            <Box my={3}>
                <List
                    sx={{ width: '100%', mt: 2, borderTop: `1px solid ${grey[300]}` }}
                >

                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information" color="secondary" onClick={showAccountDetails(sampleAccount)}>
                                <MoreHoriz />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined}>
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Account 1</Typography>} secondary={'•••• 0060'} />
                        </ListItemButton>
                    </ListItem>
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information" color="secondary" onClick={showAccountDetails(sampleAccount)}>
                                <MoreHoriz />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} >
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Account 2</Typography>} secondary={'•••• 0055'} />
                        </ListItemButton>
                    </ListItem>
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information" color="secondary" onClick={showAccountDetails(sampleAccount)}>
                                <MoreHoriz />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} >
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Account 1</Typography>} secondary={'•••• 0080'} />
                        </ListItemButton>
                    </ListItem>
                    <ListItem
                        secondaryAction={
                            <IconButton edge="end" aria-label="more information" color="secondary" onClick={showAccountDetails(sampleAccount)}>
                                <MoreHoriz />
                            </IconButton>
                        }
                        disablePadding
                        divider={true}
                    >
                        <ListItemButton role={undefined} >
                            <ListItemText id={'test'} primary={<Typography variant="subtitle1">Account 1</Typography>} secondary={<>
                                <Typography >•••• 08</Typography>
                                <Typography variant="body2">Disabled on 21 September 2020</Typography>
                            </>}
                            />
                        </ListItemButton>
                    </ListItem>
                </List>

                <Dialog open={accountDetails.open} maxWidth='xs'>
                    <DialogActions sx={{ p: 0 }}>
                        <Button autoFocus onClick={onClose} startIcon={<Close />} variant="contained" sx={{ borderRadius: 0 }}>
                            Close
                        </Button>
                    </DialogActions>
                    <DialogContent>
                        <AccountInfo account={accountDetails.account} />
                    </DialogContent>
                </Dialog>
            </Box>

        </InternalLayout>
    )
}

/** TODO: - move dialog to new component. dialog full screen on mobile */