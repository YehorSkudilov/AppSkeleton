using System.Diagnostics;

#if ANDROID || IOS
using Plugin.AdMob.Services;
using Plugin.AdMob;
#endif

namespace AppSkeleton
{
    class AdManager
    {
#if ANDROID || IOS
        private readonly IRewardedAdService _rewardedAdService;
#endif
        public AdManager()
        {
#if ANDROID || IOS

            _rewardedAdService = ServiceProvider.GetRequiredService<IRewardedAdService>();

            _rewardedAdService.OnAdLoaded += (_, __) => Debug.WriteLine("Rewarded ad prepared.");
            _rewardedAdService.PrepareAd(onUserEarnedReward: UserDidEarnReward);
#endif

        }

        public void LoadAd()
        {
#if ANDROID || IOS
            var rewardedAd = _rewardedAdService.CreateAd();
            rewardedAd.OnAdLoaded += RewardedAd_OnAdLoaded;

            rewardedAd.OnUserEarnedReward += (_, reward) =>
            {
                UserDidEarnReward(reward);
            };
            rewardedAd.Load();
#endif

        }

        private void RewardedAd_OnAdLoaded(object? sender, EventArgs e)
        {
#if ANDROID || IOS

            if (sender is IRewardedAd rewardedAd)
            {
                rewardedAd.Show();
            }
#endif
        }
#if ANDROID || IOS

        private void UserDidEarnReward(RewardItem rewardItem)
        {
            Debug.WriteLine($"User earned {rewardItem.Amount} {rewardItem.Type}.");
            // Replace these with actual references to your UI or state management
            // GetPointsButton.IsEnabled = true;
            // Coins += 10;
        }
#endif
    }
}
