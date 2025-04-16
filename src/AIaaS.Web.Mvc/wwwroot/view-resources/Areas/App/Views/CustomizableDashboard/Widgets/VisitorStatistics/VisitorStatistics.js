(function () {
    app.widgets.Widgets_Tenant_VisitorStatistics = function () {
        var _tenantDashboardService = abp.services.app.tenantDashboard;

        var _widget;

        var _$visitorStatisticsChartContainer;
        var _visitorStatisticsData = [];


        var _selectedDateRange = {
            startDate: moment().startOf('month'),
            endDate: moment().endOf('day')
        };


        this.init = function (widgetManager) {
            _widgetManager = widgetManager;
            _widget = widgetManager.getWidget();
            _$visitorStatisticsChartContainer = _widget.find('.visitor-statistics-chart');
            initialize();
        };


        var getNoDataInfo = function () {
            return $('<div/>')
                .addClass('note')
                .addClass('note-info')
                .addClass('text-center')
                .append(
                    $('<small/>')
                        .addClass('text-muted')
                        .text('- ' + app.localize('NoData') + ' -')
                );
        };

        var drawVisitorStatisticsChart = function (data) {

            if (!data || data.length === 0) {
                _$visitorStatisticsChartContainer.html(getNoDataInfo());
                return;
            }

            var visitorStatisticsChartLastTooltipIndex = null;
            //_$visitorStatisticsCaptionHelper.text(getCurrentDateRangeText());
            var chartData = [];
            for (var i = 0; i < data.length; i++) {
                var point = new Array(2);
                point[0] = moment(data[i].date).utc().valueOf(); // data[i].label;
                point[1] = data[i].value;
                chartData.push(point);
            }

            _visitorStatisticsData = chartData;

            $.plot(_$visitorStatisticsChartContainer,
                [{
                    data: _visitorStatisticsData,
                    lines: {
                        fill: 0.2,
                        lineWidth: 1
                    },
                    color: ['#C8F1EF']
                }, {
                    data: chartData,
                    points: {
                        show: true,
                        fill: true,
                        radius: 4,
                        fillColor: '#1BC5BD',
                        lineWidth: 2
                    },
                    color: '#1BC5BD',
                    shadowSize: 1
                }, {
                    data: chartData,
                    lines: {
                        show: true,
                        fill: false,
                        lineWidth: 3
                    },
                    color: '#1BC5BD',
                    shadowSize: 0
                }],
                {
                    xaxis: {
                        mode: 'time',
                        timeformat: app.localize('ChartDateFormat'),
                        minTickSize: [1, 'day'],
                        font: {
                            lineHeight: 20,
                            style: 'normal',
                            variant: 'small-caps',
                            color: '#6F7B8A',
                            size: 10
                        }
                    },
                    yaxis: {
                        ticks: 5,
                        tickDecimals: 0,
                        tickColor: '#eee',
                        font: {
                            lineHeight: 14,
                            style: 'normal',
                            variant: 'small-caps',
                            color: '#6F7B8A'
                        }
                    },
                    grid: {
                        hoverable: true,
                        clickable: false,
                        tickColor: '#eee',
                        borderColor: '#eee',
                        borderWidth: 1,
                        margin: {
                            bottom: 20
                        }
                    }
                });

            var removeChartTooltipIfExists = function () {
                var $chartTooltip = $('#chartTooltip');
                if ($chartTooltip.length === 0) {
                    return;
                }

                $chartTooltip.remove();
            };

            var showChartTooltip = function (x, y, label, value) {
                removeChartTooltipIfExists();
                $("<div id='chartTooltip' class='chart-tooltip'>" + label + "<br/>" + value + "</div >")
                    .css({
                        position: 'absolute',
                        display: 'none',
                        top: y - 60,
                        left: x - 40,
                        border: '0',
                        padding: '2px 6px',
                        opacity: '0.9'
                    })
                    .appendTo('body')
                    .fadeIn(200);
            };

            _$visitorStatisticsChartContainer.bind('plothover', function (event, pos, item) {
                if (!item) {
                    return;
                }

                if (visitorStatisticsChartLastTooltipIndex !== item.dataIndex) {
                    //var interval = getSelectedVisitorStatisticsDatePeriod();
                    var label = '';
                    var isSingleDaySelected = _selectedDateRange.startDate.format('L') === _selectedDateRange.endDate.format('L');

                    if (isSingleDaySelected) {
                        label = moment(item.datapoint[0]).format('dddd, DD MMMM YYYY');
                    }
                    else {
                        var isLastItem = item.dataIndex === item.series.data.length - 1;
                        label += moment(item.datapoint[0]).format('LL');
                        if (isLastItem) {
                            label += ' - ' + _selectedDateRange.endDate.format('LL');
                        } else {
                            var nextItem = item.series.data[item.dataIndex + 1];
                            label += ' - ' + moment(nextItem[0]).format('LL');
                        }
                    }

                    visitorStatisticsChartLastTooltipIndex = item.dataIndex;
                    var value = app.localize('VisitorCount', '<strong>' + item.datapoint[1] + '</strong>');
                    showChartTooltip(item.pageX, item.pageY, label, value);
                }
            });

            _$visitorStatisticsChartContainer.bind('mouseleave', function () {
                visitorStatisticsChartLastTooltipIndex = null;
                removeChartTooltipIfExists();
            });
        };

        var getAndDrawVisitorStatisticsData = function () {
            abp.ui.setBusy(_widget);
            _tenantDashboardService
                .getVisitorStatistics({
                    startDate: _selectedDateRange.startDate.format('YYYY-MM-DDT00:00:00Z'),
                    endDate: _selectedDateRange.endDate.format('YYYY-MM-DDT23:59:59.999Z'),
                    visitorStatisticsDateInterval: 1
                })
                .done(function (result) {
                    drawVisitorStatisticsChart(result.visitorStatistics);
                }).always(function () {
                    abp.ui.clearBusy(_widget);
                });
        };

        var refreshAllData = function () {
            getAndDrawVisitorStatisticsData();
        };


        var initialize = function () {
            _widgetManager.runDelayed(refreshAllData);
            setInterval(refreshAllData, 5 * 60 * 1000);
        };
	
	
        abp.event.on('app.dashboardFilters.DateRangePicker.OnDateChange', function (dateRange) {
            if (
                !_widget ||
                !dateRange ||
                (_selectedDateRange.startDate === dateRange.startDate && _selectedDateRange.endDate === dateRange.endDate)
            ) {
                return;
            }
            _selectedDateRange.startDate = dateRange.startDate;
            _selectedDateRange.endDate = dateRange.endDate;

            _widgetManager.runDelayed(refreshAllData);
        });
    };
})();
