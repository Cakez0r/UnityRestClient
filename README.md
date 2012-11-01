# Unity Rest Client
A small library for interacting with JSON REST APIs from within a Unity3D project. Comes with an example that uses Twitter's search API.

##Example syntax
`m_searchResult = m_restClient.Get<SearchResponse>(endpoint, new { q = m_searchQuery, rpp = m_resultsPerPage });`