import { createStyles, makeStyles } from '@material-ui/core';

export const useStyles = makeStyles(() => createStyles({
    nav: {
        display: 'flex',
        paddingTop: '25px',
        position: 'fixed',
        width: '100%',
        overflow: 'hidden',
        background: 'white',
    },
    logo: {
        fontSize: '25px',
        textDecoration: 'none',
        color: 'black'
    },
    result: {
        paddingBottom: '20px'
    }
}));