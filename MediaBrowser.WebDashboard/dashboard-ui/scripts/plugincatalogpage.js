﻿(function ($, document) {

    // The base query options
    var query = {
        TargetSystems: 'Server',
        IsAdult: false
    };

    function reloadList(page) {

        Dashboard.showLoadingMsg();

        if (AppInfo.enableAppStorePolicy) {
            $('.optionAdultContainer', page).hide();
        } else {
            $('.optionAdultContainer', page).show();
        }

        query.IsAppStoreSafe = true;

        var promise1 = ApiClient.getAvailablePlugins(query);

        var promise2 = ApiClient.getInstalledPlugins();

        Promise.all([promise1, promise2]).then(function (responses) {

            populateList({

                catalogElement: $('#pluginTiles', page),
                noItemsElement: $("#noPlugins", page),
                availablePlugins: responses[0],
                installedPlugins: responses[1]

            });
        });
    }
    function populateList(options) {
        requirejs(['scripts/ratingdialog'], function () {
            populateListInternal(options);
        });
    }

    function populateListInternal(options) {

        var availablePlugins = options.availablePlugins;
        var installedPlugins = options.installedPlugins;

        var allPlugins = availablePlugins.filter(function (p) {

            p.category = p.category || "General";
            p.categoryDisplayName = Globalize.translate('PluginCategory' + p.category.replace(' ', ''));

            if (options.categories) {
                if (options.categories.indexOf(p.category) == -1) {
                    return false;
                }
            }

            if (options.targetSystem) {
                if (p.targetSystem != options.targetSystem) {
                    return false;
                }
            }

            return p.type == "UserInstalled";

        });

        availablePlugins = allPlugins.sort(function (a, b) {

            var aName = (a.category);
            var bName = (b.category);

            if (aName > bName) {
                return 1;
            }
            if (bName > aName) {
                return -1;
            }

            aName = (a.name);
            bName = (b.name);

            if (aName > bName) {
                return 1;
            }
            if (bName > aName) {
                return -1;
            }

            return 0;
        });

        var html = '';
        var i, length, plugin;

        var currentCategory;

        if (!options.categories) {
            currentCategory = Globalize.translate('HeaderTopPlugins');
            html += '<div class="detailSectionHeader">' + currentCategory + '</div>';
            var topPlugins = allPlugins.slice(0).sort(function (a, b) {

                if (a.installs > b.installs) {
                    return -1;
                }
                if (b.installs > a.installs) {
                    return 1;
                }

                var aName = (a.name);
                var bName = (b.name);

                if (aName > bName) {
                    return 1;
                }
                if (bName > aName) {
                    return -1;
                }

                return 0;
            });

            var limit = screen.availWidth >= 1920 ? 15 : 12;
            for (i = 0, length = Math.min(topPlugins.length, limit) ; i < length; i++) {
                html += getPluginHtml(topPlugins[i], options, installedPlugins);
            }
        }

        for (i = 0, length = availablePlugins.length; i < length; i++) {

            plugin = availablePlugins[i];

            var category = plugin.categoryDisplayName;

            if (category != currentCategory) {

                if (options.showCategory !== false) {
                    if (currentCategory) {
                        html += '<br/>';
                        html += '<br/>';
                        html += '<br/>';
                    }

                    html += '<div class="detailSectionHeader">' + category + '</div>';
                }

                currentCategory = category;
            }

            html += getPluginHtml(plugin, options, installedPlugins);

        }

        if (!availablePlugins.length && options.noItemsElement) {
            $(options.noItemsElement).hide();
        }

        $(options.catalogElement).html(html);

        Dashboard.hideLoadingMsg();
    }

    function getPluginHtml(plugin, options, installedPlugins) {

        var html = '';

        var href = plugin.externalUrl ? plugin.externalUrl : "addplugin.html?name=" + encodeURIComponent(plugin.name) + "&guid=" + plugin.guid;
        if (options.context) {
            href += "&context=" + options.context;
        }
        var target = plugin.externalUrl ? ' target="_blank"' : '';

        html += "<div class='card backdropCard bottomPaddedCard'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        html += '<a class="cardContent" href="' + href + '"' + target + '>';
        if (plugin.thumbImage) {
            html += '<div class="cardImage" style="background-image:url(\'' + plugin.thumbImage + '\');">';
        } else {
            html += '<div class="cardImage" style="background-image:url(\'css/images/items/list/collection.png\');">';
        }

        if (plugin.isPremium) {
            if (plugin.price > 0) {
                html += "<div class='premiumBanner'><img src='css/images/supporter/premiumflag.png' /></div>";
            } else {
                html += "<div class='premiumBanner'><img src='css/images/supporter/supporterflag.png' /></div>";
            }
        }
        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += "<div class='cardText'>";
        html += plugin.name;
        html += "</div>";

        if (!plugin.isExternal) {
            html += "<div class='cardText packageReviewText'>";
            html += plugin.price > 0 ? "$" + plugin.price.toFixed(2) : Globalize.translate('LabelFree');
            html += RatingHelpers.getStoreRatingHtml(plugin.avgRating, plugin.id, plugin.name, true);

            html += "<span class='storeReviewCount'>";
            html += " " + Globalize.translate('LabelNumberReviews').replace("{0}", plugin.totalRatings);
            html += "</span>";

            html += "</div>";
        }

        var installedPlugin = plugin.isApp ? null : installedPlugins.filter(function (ip) {
            return ip.Id == plugin.guid;
        })[0];

        html += "<div class='cardText'>";

        if (installedPlugin) {
            html += Globalize.translate('LabelVersionInstalled').replace("{0}", installedPlugin.Version);
        } else {
            html += '&nbsp;';
        }
        html += "</div>";

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    $(document).on('pageinit', "#pluginCatalogPage", function () {

        var page = this;

        $('.chkPremiumFilter', page).on('change', function () {

            if (this.checked) {
                query.IsPremium = false;
            } else {
                query.IsPremium = null;
            }
            reloadList(page);
        });

        $('.radioPackageTypes', page).on('change', function () {

            var val = $('.radioPackageTypes:checked', page).val();

            query.TargetSystems = val;
            reloadList(page);
        });

        $('#chkAdult', page).on('change', function () {

            query.IsAdult = this.checked ? null : false;
            reloadList(page);
        });

    }).on('pageshow', "#pluginCatalogPage", function () {

        var page = this;

        $(".radioPackageTypes", page).each(function () {

            this.checked = this.value == query.TargetSystems;

        }).checkboxradio('refresh');

        // Reset form values using the last used query
        $('.chkPremiumFilter', page).each(function () {

            var filters = query.IsPremium || false;

            this.checked = filters;

        }).checkboxradio('refresh');

        reloadList(page);
    });

    window.PluginCatalog = {
        renderCatalog: populateList
    };

})(jQuery, document);