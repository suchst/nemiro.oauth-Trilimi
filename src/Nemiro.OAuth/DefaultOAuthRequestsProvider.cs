// ----------------------------------------------------------------------------
// Copyright © Aleksey Nemiro, 2016. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Nemiro.OAuth
{

  /// <summary>
  /// Represents the default provider requests.
  /// </summary>
  /// <remarks>
  /// <para>Requests are stored in the memory, in the current instance of provider.</para>
  /// <para>Used requests are automatically deleted.</para>
  /// <para>Maximum storage time of unused requests - 20 minutes.</para>
  /// </remarks>
  internal class DefaultOAuthRequestsProvider : IOAuthRequestsProvider
  {
    private Timer Timer = new Timer(60000);

    /// <summary>
    /// Gets the list of active requests.
    /// </summary>
    internal ConcurrentDictionary<string, OAuthRequest> Requests { get; private set; }

    public DefaultOAuthRequestsProvider()
    {
      this.Requests = new ConcurrentDictionary<string, OAuthRequest>();
      this.Timer.Elapsed += Timer_Elapsed;
    }

    /// <summary>
    /// The method is called when the interval elapsed.
    /// </summary>
    /// <param name="sender">Instance of the object that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Timer_Elapsed(object sender, EventArgs e)
    {
      if (this.Requests.IsEmpty)
      {
        // no active requests, stop the time
        this.Timer.Stop();
        return;
      }

      // lifetime request - 20 minutes
      // remove old requests
      var now = DateTime.Now;
      var toRemove = this.Requests.Where(itm2 => now.Subtract(itm2.Value.DateCreated).TotalMinutes >= 20).ToList();

      foreach (var itm in toRemove)
      {
        OAuthManager.RemoveRequest(itm.Key);
      }

      // change the status of the timer
      this.Timer.Enabled = !this.Requests.IsEmpty;
    }

    /// <summary>
    /// Adds the specified request to the collection.
    /// </summary>
    /// <param name="key">The unique key of request.</param>
    /// <param name="clientName">The client name.</param>
    /// <param name="client">The client instance.</param>
    /// <param name="state">Custom state associated with authorization request.</param>
    public void Add(string key, ClientName clientName, OAuthBase client, object state)
    {
      if (String.IsNullOrEmpty(key))
      {
        throw new ArgumentNullException("key");
      }

      if (!this.Requests.TryAdd(key, new OAuthRequest(clientName, client, state)))
      {
        throw new ArgumentException($"A request with the key '{key}' already exists.");
      }

      this.Timer.Start();
    }

    /// <summary>
    /// Determines whether the collection contains a request with the specified key.
    /// </summary>
    /// <param name="key">The unique key of request.</param>
    /// <returns><c>true</c> if the collection contains a request with the specified key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(string key)
    {
      return this.Requests.ContainsKey(key);
    }

    /// <summary>
    /// Gets the request with the specified key.
    /// </summary>
    /// <param name="key">The unique key of request.</param>
    /// <returns>The request with the specified key.</returns>
    public OAuthRequest Get(string key)
    {
      return this.Get<OAuthRequest>(key);
    }

    /// <summary>
    /// Gets the request with the specified key and type.
    /// </summary>
    /// <typeparam name="T">The type of request.</typeparam>
    /// <param name="key">The unique key of request.</param>
    /// <returns>The request with the specified key and type.</returns>
    public T Get<T>(string key) where T : OAuthRequest
    {
      if (String.IsNullOrEmpty(key))
      {
        throw new ArgumentNullException("key");
      }

      if (this.Requests.TryGetValue(key, out var value))
      {
        return (T)value;
      }
      throw new KeyNotFoundException($"No request found for key '{key}'.");
    }

    /// <summary>
    /// Removes the request with the specified key from the collection.
    /// </summary>
    /// <param name="key">The unique key of request.</param>
    public void Remove(string key)
    {
      if (String.IsNullOrEmpty(key))
      {
        throw new ArgumentNullException("key");
      }

      this.Requests.TryRemove(key, out _);
      this.Timer.Enabled = !this.Requests.IsEmpty;
    }

    /// <summary>
    /// Removes all requests from the collection.
    /// </summary>
    public void Clear()
    {
      this.Requests.Clear();
      this.Timer.Enabled = false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator GetEnumerator()
    {
      // Return a snapshot to avoid enumeration issues
      return this.Requests.ToList().GetEnumerator();
    }

  }

}