﻿using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindings : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IISBindingsOptions _options;
        private readonly IISBindingHelper _helper;
        private readonly UserRoleService _userRoleService;

        public IISBindings(
            ILogService logService, UserRoleService roleService,
            IISBindingHelper helper, IISBindingsOptions options)
        {
            _log = logService;
            _options = options;
            _helper = helper;
            _userRoleService = roleService;
        }

        public async Task<Target?> Generate()
        {
            // Check if we have any bindings
            var bindings = _helper.FilterBindings(_options);
            if (bindings.Count() == 0)
            {
                return null;
            }

            // Generate friendly name suggestion
            var friendlyNameSuggestion = "[IIS]";
            if (_options.IncludeSiteIds != null && _options.IncludeSiteIds.Any())
            {
                var sites = string.Join(',', _options.IncludeSiteIds);
                friendlyNameSuggestion += $" site {sites}";
            } 
            else
            {
                friendlyNameSuggestion += $" all sites";
            }

            if (!string.IsNullOrEmpty(_options.IncludePattern))
            {
                friendlyNameSuggestion += $" {_options.IncludePattern}";
            }
            else if (_options.IncludeHosts != null && _options.IncludeHosts.Any())
            {
                var hosts = string.Join(',', _options.IncludeHosts);
                friendlyNameSuggestion += $" {hosts}";
            }
            else if (_options.IncludeRegex != null)
            {
                friendlyNameSuggestion += $" {_options.IncludeRegex}";
            }
            else
            {
                friendlyNameSuggestion += $" all hosts";
            }

            // Return result
            var result = new Target()
            {
                FriendlyName = friendlyNameSuggestion,
                CommonName = _options.CommonName ?? bindings.First().HostUnicode,
                Parts = bindings.
                    GroupBy(x => x.SiteId).
                    Select(group => new TargetPart
                    {
                        SiteId = group.Key,
                        Identifiers = group.Select(x => x.HostUnicode).ToList()
                    }).
                    ToList()
            };
            return result;
        }

        bool IPlugin.Disabled => Disabled(_userRoleService);
        internal static bool Disabled(UserRoleService userRoleService) => !userRoleService.AllowIIS;
    }
}
