// Copyright 2011-2013 Anodyne.
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

namespace Kostassoid.Anodyne.Abstractions.Dependency.Registration
{
    using System;

    public interface ISingleBindingSyntax : IBindingSyntax
    {
        ISingleBindingSyntax As<TService>() where TService : class;
        ISingleBindingSyntax As(Type service);
        ISingleBindingSyntax With(Lifestyle lifestyle);
        ISingleBindingSyntax Named(string name);
    }

    public interface ISingleBindingSyntax<in TImpl> : IBindingSyntax where TImpl : class
    {
		ISingleBindingSyntax<TImpl> As<TService>() where TService : class;
		ISingleBindingSyntax<TImpl> As(Type service);
        ISingleBindingSyntax<TImpl> With(Lifestyle lifestyle);
        ISingleBindingSyntax<TImpl> Named(string name);
    }
}