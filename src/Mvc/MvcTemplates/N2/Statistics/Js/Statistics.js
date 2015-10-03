﻿function PageStatsCtrl($rootScope, $scope, $resource) {
	var res = $resource('Api/Content.ashx/:target', { target: 'statistics' }, {
		'statistics': { method: 'GET', params: { target: 'statistics' } },
	});
	function getMaximum(localMax, globalMax) {
		while (localMax > globalMax)
			globalMax *= 10;
		return globalMax;
	}
	$scope.max = 1;
	$rootScope.$on("contextchanged", function (scope, args) {
		$scope.loading = true;
		res.statistics({ n2item: args.CurrentItem.ID }, function (result) {
			$rootScope.statisticsMaximum = getMaximum(result.Max, $rootScope.statisticsMaximum || 1)
			$scope.max = $rootScope.statisticsMaximum;
			$scope.Statistics = result;
			$scope.loading = false;
		});
	});
};