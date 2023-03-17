import { Box, List, ListItem, ListItemText, Typography } from "@mui/material"
import { grey } from "@mui/material/colors"
import { InternalLayout } from "../../components/InternalLayout"
import { useRecoilValue } from 'recoil';
import { DataRecipientName } from "../../state/Common.state"

export default function SharingPeriod() {

    const dataRecipientName = useRecoilValue(DataRecipientName);

    return (
        <InternalLayout selectedMenu="settings" pageTitle="Sharing period">
            <Box my={3}>
                <Typography color={'text.secondary'} variant="button">
                    CONSENT SHARING PERIOD
                </Typography>
                <List sx={{ width: '100%', mt: 1, borderTop: `1px solid ${grey[300]}` }}>
                    <ListItem disableGutters divider={true}>
                        <ListItemText primary={<Typography variant="body1">Shared on</Typography>} />
                        <Typography variant="body1">1 June 2020</Typography>
                    </ListItem>
                    <ListItem disableGutters divider={true}>
                        <ListItemText primary={<Typography variant="body1">Shared until</Typography>} />
                        <Typography variant="body1">31 May 2021</Typography>
                    </ListItem>
                    <ListItem disableGutters divider={true}>
                        <ListItemText primary={<Typography color={'text.secondary'} variant="body2">{dataRecipientName} can access the data you've authorised on an ongoing basis for 12 months.</Typography>} />
                    </ListItem>
                </List>
            </Box>
            <Box my={3}>
                <Typography color={'text.secondary'} variant="button">
                    HISTORICAL DATA
                </Typography>
                <List sx={{ width: '100%', mt: 1, borderTop: `1px solid ${grey[300]}` }}>
                    <ListItem disableGutters divider={true}>
                        <ListItemText primary={<Typography variant="body1">
                            you have shared data that may date back to [1 January 2017].
                        </Typography>} />
                    </ListItem>
                </List>
            </Box>
        </InternalLayout >
    )
}
