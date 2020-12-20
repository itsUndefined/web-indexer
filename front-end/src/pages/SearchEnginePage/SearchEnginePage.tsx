import React                                                  from 'react';
import { IconButton, InputAdornment, TextField } from '@material-ui/core';
import { Search }                                             from '@material-ui/icons';
import { useStyles }                                          from './styles';

export const SearchEnginePage = () => {
    const styles = useStyles();

    return (
        <div className={styles.search}>
            <div style={{width: '500px'}}>
                <div className={styles.logo}>My Search</div>
                <TextField id={'search'} fullWidth autoFocus placeholder={'Search bar'} autoComplete={'off'} variant={'outlined'} InputProps={{
                    endAdornment: (
                        <InputAdornment position={'end'}>
                            <IconButton edge={'end'}>
                                <Search/>
                            </IconButton>
                        </InputAdornment>
                    )
                }}>
                </TextField>
            </div>
        </div>
    );
};