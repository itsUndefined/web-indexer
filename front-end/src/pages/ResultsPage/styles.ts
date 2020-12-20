import { createStyles, makeStyles } from '@material-ui/core';

export const useStyles = makeStyles(() => createStyles({
    style: {
        paddingTop: '25px',
        paddingLeft: '25px'
    },
    nav: {
        display: 'flex',
        paddingBottom: '50px'
    },
    logo: {
        fontSize: '25px'
    },
    result: {
        paddingBottom: '20px'
    }
}));