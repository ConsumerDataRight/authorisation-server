import { Box } from "@mui/material"
import ClusterList from "../../components/ClusterList"
import { InternalLayout } from "../../components/InternalLayout"

export default function ScopeList() {

    return (
        <InternalLayout selectedMenu="settings" pageTitle="Data requested">
            <Box my={3}>
                <ClusterList scopes={"bank:accounts.basic:read bank:accounts.detail:read email profile name"} />
            </Box>
        </InternalLayout>
    )
}