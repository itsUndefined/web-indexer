import React, { useEffect, useRef, useState }                                     from 'react';
import { useStyles }                                     from './styles';
import { Button, Container, IconButton, InputAdornment, LinearProgress, TextField } from '@material-ui/core';
import { Search }                                        from '@material-ui/icons';
import axios from 'axios';
import { useHistory, useLocation } from 'react-router-dom';



const useQuery = () => new URLSearchParams(useLocation().search);

export const ResultsPage = () => {
    const styles = useStyles();

    
    const history = useHistory();
    const q = useQuery().get('q');
    const [query, setQuery] = useState<string>(q!);
    
    
    const [results, setResults] = useState<any[]>([]);

    const [isLoading, setIsLoading] = useState(true);

    const [hasFeedback, setHasFeedback] = useState(false);


    const feedback = useRef<{ id: number; feedback: 'yes' | 'no' }[]>([]);

    const changeFeedback = (docId: number, feedbackOption: null | 'yes' | 'no') => {
        const index = feedback.current.findIndex(x => x.id === docId);
        if (index !== -1 && !feedbackOption) {
            feedback.current.splice(index , 1);
        } else if (index !== -1 && feedbackOption) {
            feedback.current[index].feedback = feedbackOption;
        } else {
            if(!feedbackOption) return;
            feedback.current.push({
                id: docId,
                feedback: feedbackOption
            });
        }

        setHasFeedback(feedback.current.length !== 0);
    }

    const doSearch = () => {
        axios.get('http://localhost:5000/Documents', { params: { q: query }}).then(response => {
            setResults(response.data);
            setIsLoading(false);
            history.push(`/results?q=${query}`);
        });
    };

    const searchWithFeedback = () => {
        setIsLoading(true);
        axios.get('http://localhost:5000/Documents/search-with-feedback', { 
            params : {
                q: q,
                p: feedback.current.filter(x => x.feedback === 'yes').map(x => x.id),
                n: feedback.current.filter(x => x.feedback === 'no').map(x => x.id),
            }
        }).then(response => {
            console.log(response.data);
            setResults(response.data);
            setIsLoading(false);
        });
    };

    useEffect(() => {
        axios.get('http://localhost:5000/Documents', { params: { q }}).then(response => {
            setResults(response.data);
            setIsLoading(false);
        });
    }, []);

    // const results = [{id: 1, title: 'Title1', abstract: 'Abstract1'}, {id: 2, title: 'Title2', abstract: 'Abstract2'}, {id: 3, title: 'Title2', abstract: 'Abstract3'}];

    const resultComponent = results.map((result) => (
        <div className={styles.result} key={result.document.id}>
            <div><a href={result.document.url}>{ result.document.title }</a></div>
            <div>
                Is the content relative? &nbsp;
                <select onChange={(e) => changeFeedback(result.document.id, e.target.value as any)}>
                    <option selected></option>
                    <option value="yes">Yes</option>
                    <option value="no">No</option>
                </select>
            </div>
            {/* <span style={{fontSize: '14px'}}>{ result.abstract }</span> */}
        </div>
    ));

    return (
        <>
            {isLoading ? <LinearProgress color="secondary" /> : null}
            <Container>
                <div className={styles.nav}>
                    <div><a href={'/'} className={styles.logo}>My Search</a></div>
                    <TextField 
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        id={'search'}
                        style={{width: '500px', paddingLeft: '15px', paddingRight: '15px'}}
                        fullWidth
                        autoFocus
                        placeholder={'Search bar'} 
                        autoComplete={'off'} 
                        variant={'outlined'} 
                        onKeyPress={(e) => { if(e.key === 'Enter') doSearch() }}
                        InputProps={{
                            endAdornment: (
                                <InputAdornment position={'end'}>
                                    <IconButton  onClick={doSearch} edge={'end'}>
                                        <Search/>
                                    </IconButton>
                                </InputAdornment>
                            )
                        }}
                    >
                    </TextField>
                    {hasFeedback ? <Button onClick={searchWithFeedback} disabled={isLoading} type={'submit'} variant={'contained'} color={'primary'}>
                        Submit your feedback
                    </Button> : null}
                </div>
                {
                    !isLoading ? 
                    <div style={{paddingTop: '125px'}}>
                        {resultComponent.length ? resultComponent : 'Δεν βρέθηκαν αποτελέσματα'}
                    </div> :
                    null
                }
            </Container>
        </>
    );
};
