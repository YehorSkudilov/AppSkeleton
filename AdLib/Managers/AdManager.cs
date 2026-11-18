

#if ANDROID || IOS
using Microsoft.Extensions.DependencyInjection;
using Plugin.AdMob.Services;
using Plugin.AdMob;
using System.Diagnostics;
#endif

namespace AppSkeleton
{
    class AdManager
    {    
        Action CallBack;

#if ANDROID || IOS
        private readonly IRewardedAdService _rewardedAdService;
#endif
        public AdManager(Action callBack)
        {
            CallBack = callBack;
#if ANDROID || IOS

            // Plugin.AdMob 11 dropped its own static ServiceProvider: the app's services (UseAdMob registers it there).
            _rewardedAdService = IPlatformApplication.Current!.Services.GetRequiredService<IRewardedAdService>();

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
            CallBack.Invoke();
        }
#endif
    }
}
