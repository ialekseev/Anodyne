﻿// Copyright 2011-2012 Anodyne.
//   
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
//  
//      http://www.apache.org/licenses/LICENSE-2.0 
//  
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

namespace Kostassoid.BlogNote.Host.Domain
{
    using Anodyne.Domain.Events;

    public class PostCreatedEvent : AggregateEvent<Post, PostCreatedEvent.EventData>
    {
        public PostCreatedEvent(Post aggregate, BasePostContent content)
            : base(aggregate, new EventData(content))
        {
        }

        public class EventData : Anodyne.Domain.Events.EventData
        {
            public BasePostContent Content { get; protected set; }

            public EventData(BasePostContent content)
            {
                Content = content;
            }
        }

    }

}