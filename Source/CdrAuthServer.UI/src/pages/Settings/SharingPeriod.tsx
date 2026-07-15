import { Box, List, ListItem, ListItemText, Typography } from "@mui/material"
import { grey } from "@mui/material/colors"
import { InternalLayout } from "../../components/InternalLayout"
import { useCommonContext } from "../../context/CommonContext";

export default function SharingPeriod() {

    const { commonState } = useCommonContext();
    const dataRecipientName = commonState.dataRecipient?.BrandName;

    return (
        <InternalLayout selectedMenu="settings" pageTitle="Sharing period">
            <Box sx={{ my: 3 }}>
                <Typography variant="button" sx={{
                    color: 'text.secondary'
                }}>
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
                        <ListItemText primary={<Typography variant="body2" sx={{
                            color: 'text.secondary'
                        }}>{dataRecipientName} can access the data you've authorised on an ongoing basis for 12 months.</Typography>} />
                    </ListItem>
                </List>
            </Box>
            <Box sx={{ my: 3 }}>
                <Typography variant="button" sx={{
                    color: 'text.secondary'
                }}>
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
    );
}
