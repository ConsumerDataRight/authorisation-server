import { ExpandMore } from "@mui/icons-material";
import { AccordionProps, Accordion, AccordionSummaryProps, AccordionSummary, AccordionDetails, Typography, Link } from "@mui/material";
import { grey } from "@mui/material/colors";
import { useEffect, useState } from "react";
import styled from "styled-components";
import { useData } from "../hooks/useData";
import { ClusterModel } from "../models/DataModels";

export default function ClusterList({ scopes }: { scopes: string }) {
    const [openedClusterIds, setOpenedClusterIds] = useState<string[]>([]);
    const [clusters, setClusters] = useState<ClusterModel[]>([]);
    const { getAllClusters } = useData();

    useEffect(() => {
        getAllClusters().then((clusters: ClusterModel[] | null) => {
            if (clusters == null || clusters.length === 0 || scopes === "") {
                return;
            }

            // Run the cluster matching logic based on the scopes provided
            var matchingClusters = clusters.filter(c => {
                // Check if this cluster matches the provided scope match
                var scopeMatches = c.ScopeMatch.split('|');
                var matchesList = scopeMatches.filter(cm => new RegExp(`${cm}( |$)`).test(scopes))
                if (matchesList.length === 0) {
                    return false;
                }

                // Now, check the contain and does not contain conditions
                if (c.IncludeWhenContains) {
                    return new RegExp(`${c.IncludeWhenContains}( |$)`).test(scopes);
                }
                if (c.IncludeWhenDoesNotContain) {
                    return new RegExp(`${c.IncludeWhenDoesNotContain}( |$)`).test(scopes) === false;
                }

                return true;
            });
            setClusters(matchingClusters);
        });
    }, [scopes]);

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

    const CdrAccordion = styled((props: AccordionProps) => (
        <Accordion disableGutters elevation={0} square {...props} />
    ))(() => ({
        border: `1px solid ${grey[200]}`,
        '&:not(:last-child)': {
            borderBottom: 0,
        },
        '&:before': {
            display: 'none',
        },
    }));

    const CdrAccordionSummary = styled((props: AccordionSummaryProps) => (
        <AccordionSummary
            expandIcon={<ExpandMore />}
            {...props}
        />
    ))(() => ({
        backgroundColor: grey[50]
    }));

    return (
        <>
            {
                clusters.map((cluster, index) => (
                    <CdrAccordion
                        key={`cluster-${index}`}
                        expanded={openedClusterIds.includes(`cluster-${index}`)} onChange={toggleClusters(`cluster-${index}`)}>
                        <CdrAccordionSummary>
                            {/* <Button component={Link}>{cluster.DataCluster}</Button> */}
                            <Link>{cluster.DataCluster}</Link>
                            {/* <Typography></Typography> */}
                        </CdrAccordionSummary>
                        <AccordionDetails>
                            <Typography sx={{ whiteSpace: 'pre-line' }}>
                                {cluster.Permissions}
                            </Typography>
                        </AccordionDetails>
                    </CdrAccordion>
                ))
            }
        </>
    )
}