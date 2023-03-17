import { Typography, Alert } from "@mui/material";
import { AccountModel } from "../models/DataModels";

export function AccountInfo({ account, isBasic = true }: { account?: AccountModel, isBasic?: boolean }) {
    return (
        <>
            <Typography color="inherit" variant="h6">
                {account?.DisplayName}
            </Typography>
            {(account?.MaskedName || account?.AccountNumber) && <Typography color="inherit" variant="body1">
                {account?.MaskedName ?? account?.AccountNumber}
            </Typography>}
            {account?.ProductName && <Typography color="inherit" variant="body2" pb={2}>
                This account refers to {account?.ProductName}
            </Typography>}
            {isBasic === false && <>
                <Alert variant="outlined" severity="success">
                    <Typography color="text.primary" variant="body1" fontWeight={'bold'} >
                        [ADR Brand] is accessing data from this account
                    </Typography>
                </Alert>
                <Typography color="inherit" variant="body2" pt={1}>
                    Access granted on 01 June 2020
                </Typography>
                <Typography color={'text.secondary'} variant="body2">
                    Expires on 31 May 2021
                </Typography>
            </>}
        </>
    )
}