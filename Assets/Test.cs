using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    [SerializeField]
    private RESTClient m_restClient = null;

    [SerializeField]
    private string m_searchQuery = "Unity3D";

    [SerializeField]
    private int m_resultsPerPage = 10;

    private Future<SearchResponse> m_searchResult;

    private bool m_completed;

    void Start()
    {
        //We'll use Twitter's search api as an example. See https://dev.twitter.com/docs/api/1/get/search
        Uri endpoint = new Uri("http://search.twitter.com/search.json");

        Debug.Log("Searching for a maximum of " + m_resultsPerPage + " tweets about " + m_searchQuery);

        //Fire off the request!
        m_searchResult = m_restClient.Get<SearchResponse>(endpoint, new { q = m_searchQuery, rpp = m_resultsPerPage });
    }

    void Update()
    {
        if (!m_completed)
        {
            //Wait for the operation to complete and display the result via Debug.Log
            if (m_searchResult.State == FutureState.Completed)
            {
                SearchResponse response = m_searchResult.Result;

                Debug.Log("Received a response of " + response.results.Count + " tweets.");

                foreach (SearchResult tweet in response.results)
                {
                    Debug.Log(tweet.from_user_name + ": " + tweet.text);
                }
            }
            else if (m_searchResult.State == FutureState.Faulted)
            {
                Debug.LogError("Something went wrong: " + m_searchResult.Exception);
            }

            m_completed = m_searchResult.State != FutureState.Pending;
        }
    }
}
