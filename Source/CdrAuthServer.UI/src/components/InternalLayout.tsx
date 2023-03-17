import { ArrowBack, Home, Settings } from "@mui/icons-material";
import { AppBar, BottomNavigation, BottomNavigationAction, Button, Container, Grid, Link, Paper, Toolbar, Typography } from "@mui/material";
import { grey } from "@mui/material/colors";
import { useState } from "react";
import { useRecoilValue } from 'recoil';
import { DataHolderName } from "../state/Common.state";
import { Link as RouterLink } from "react-router-dom";
import { useHistory } from "react-router-dom";

export function InternalLayout({ children, selectedMenu, pageTitle="" }: { children: any, selectedMenu: string, pageTitle:string }) {
    const dataHolderName = useRecoilValue(DataHolderName);
    const [selectedPage, setSelectedPage] = useState(selectedMenu);
    const history = useHistory();

    const goBack = () => {
        history.goBack();
    }

    return (
        <Container maxWidth={'xs'}>
            <AppBar color="inherit">
                <Container maxWidth={'xs'}>
                    <Toolbar disableGutters component={Grid} container>
                        <Grid item xs={3}>
                            <Button sx={{ p: 0 }} component={Link} startIcon={<ArrowBack />} underline="always" onClick={goBack}>Back</Button>
                        </Grid>
                        <Grid item xs={6}>
                            <Typography variant="h6" textAlign={'center'}>{pageTitle}</Typography>
                        </Grid>
                        <Grid item xs={3}>
                            <Typography>&nbsp;</Typography>
                        </Grid>
                    </Toolbar>
                </Container>
            </AppBar>
            <Toolbar />
            <Grid py={2}>
                <Grid item xs={12}>
                    {children}
                </Grid>
            </Grid>
            <BottomNavigation />
            <Paper sx={{ position: 'fixed', bottom: 0, left: 0, right: 0, borderTop: `1px solid ${grey[300]}` }}>
                <BottomNavigation showLabels
                    value={selectedPage}
                    onChange={(event, newValue) => {
                        console.log(newValue);
                        setSelectedPage(newValue);
                    }}
                >
                    <BottomNavigationAction value="home" label="Home" icon={<Home />} />
                    <BottomNavigationAction component={RouterLink} to="/settings" value="settings" label="Settings" icon={<Settings />} />
                </BottomNavigation>
            </Paper>
        </Container>
    );
}

/** H6 needs to be smaller */