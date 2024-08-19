
import { Close, InfoOutlined } from "@mui/icons-material";
import { Dialog, DialogActions, DialogContent, Box, Button, Checkbox, Grid, IconButton, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Typography, FormHelperText, Link } from "@mui/material";
import { grey } from "@mui/material/colors";
import { Stack } from "@mui/system";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { AccountInfo } from "../components/AccountInfo";
import { PageLayout } from "../components/PageLayout";
import { useData } from "../hooks/useData";
import { AccountModel } from "../models/DataModels";
import settings from '../settings';
import { useCommonContext } from '../context/CommonContext';
import { useLoginContext} from '../context/LoginContext';
import { useConsentContext } from '../context/ConsentContext'; 


export default function SelectAccount() {
    const { loginState } = useLoginContext();
    const { commonState } = useCommonContext();
    const { consentState, setConsentState } = useConsentContext();
    const dataRecipientName = commonState.dataRecipient?.BrandName;
    const [selectedAccountIds, setSelectedAccountIds] = useState<string[]>([]);
    const [isSubmitted, setIsSubmitted] = useState<boolean>(false);
    const navigate = useNavigate();
    const { submitCancelConsentRequest } = useData();
    const cdrFaqLink = settings.CDR_FAQ_LINK ?? "#";


    const toggleAccountSelect = (accountId: string) => () => {
        var accountIdClone = [...selectedAccountIds];
        const existingIndex = accountIdClone.indexOf(accountId);
        if (existingIndex > -1) {
            accountIdClone.splice(existingIndex, 1);
        }
        else {
            accountIdClone.push(accountId);
        }
        setSelectedAccountIds(accountIdClone);
    }

    const selectAllAccounts = () => {
        const allAccountIds = loginState.customer?.Accounts.map(a => a.AccountId);
        if (allAccountIds && allAccountIds.length > 0) {
            setSelectedAccountIds(allAccountIds);
        }
    }

    const [accountDetails, setAccountDetails] = useState<{ open: boolean, account?: AccountModel }>({ open: false });
    const showAccountDetails = (account: AccountModel) => () => {
        setAccountDetails({ open: true, account: account });
    }
    const onAccountDetailsClose = () => {
        setAccountDetails({ open: false, account: undefined });
    }

    const onSubmit = () => {
        setIsSubmitted(true);
        if (selectedAccountIds.length === 0) {
            return;
        }

        // Save the consent related data.
        setConsentState({ ...consentState, accountIds: selectedAccountIds });

        navigate('/ui/confirmation');
    }

    const cancelRequest = () => {
        submitCancelConsentRequest();
    }

    return (
        <PageLayout>
            <Typography color="inherit" variant="h5" pb={2}>
                Select your accounts
            </Typography>
            <Typography color="inherit" variant="body2">
                {dataRecipientName} is requesting your data. Please select the accounts you would like to share data from.
            </Typography>
            <Box my={3}>
                <Stack
                    direction="row"
                    justifyContent="space-between"
                    alignItems="center"
                >
                    <Typography color={'text.secondary'} variant="subtitle2">
                        Accounts
                    </Typography>

                    <Link component={Button} size="small" onClick={selectAllAccounts}>Select all</Link>
                </Stack>
                <List
                    sx={{ width: '100%', mt: 2, borderTop: `1px solid ${grey[300]}`, backgroundColor: grey[50], mb: 1 }}
                >
                    {loginState.customer?.Accounts.map((account, index) => {
                        const labelId = `account-${account.DisplayName}`;
                        return (
                            <ListItem
                                key={`acc-${account.AccountId}`}
                                secondaryAction={
                                    <IconButton edge="end" aria-label="more information" onClick={showAccountDetails(account)}>
                                        <InfoOutlined />
                                    </IconButton>
                                }
                                disablePadding
                                divider={index + 1 === loginState.customer?.Accounts.length ? false : true}
                            >
                                <ListItemButton role={undefined} onClick={toggleAccountSelect(account.AccountId)}>
                                    <ListItemIcon>
                                        <Checkbox
                                            edge="start"
                                            checked={selectedAccountIds.indexOf(account.AccountId) !== -1}
                                            tabIndex={-1}
                                            disableRipple
                                            inputProps={{ 'aria-labelledby': labelId }}
                                        />
                                    </ListItemIcon>
                                    <ListItemText id={labelId} primary={<Typography variant="subtitle1">{account.DisplayName}</Typography>} secondary={account?.MaskedName ?? account?.AccountNumber} />
                                </ListItemButton>
                            </ListItem>
                        )
                    })}
                </List>
                {isSubmitted === true && selectedAccountIds.length === 0 && <FormHelperText error>Please select one or more Accounts</FormHelperText>}
            </Box>
            <Grid container mt={4}>
                <Grid item xs={12}>
                    <Typography color="inherit" variant="body2">
                        If you have any questions or concerns about sharing data go to <a href={cdrFaqLink} target="_blank">{cdrFaqLink}</a>
                    </Typography>
                </Grid>
                <Grid item xs={12} mt={5}>
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
                                onClick={onSubmit}
                                tabIndex={3}
                                aria-label="continue"
                            >
                                Continue
                            </Button>
                        </Grid>
                    </Grid>
                </Grid>
            </Grid>

            <Dialog open={accountDetails.open} maxWidth='xs'>
                <DialogActions sx={{ p: 0 }}>
                    <Button autoFocus onClick={onAccountDetailsClose} startIcon={<Close />} variant="contained" sx={{ borderRadius: 0 }}>
                        Close
                    </Button>
                </DialogActions>
                <DialogContent>
                    <AccountInfo account={accountDetails.account} />
                </DialogContent>
            </Dialog>

        </PageLayout>
    );
}