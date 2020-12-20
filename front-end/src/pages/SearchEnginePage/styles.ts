import { createStyles, makeStyles } from '@material-ui/core';

export const useStyles = makeStyles(() => createStyles({
    search: {
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh'
    },
    logo: {
        fontSize: '50px',
        textAlign: 'center',
        paddingBottom: '10px'
    }
}));