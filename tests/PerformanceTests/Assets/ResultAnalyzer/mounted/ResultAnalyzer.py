import pandas as pd
import seaborn as sns
import matplotlib.pyplot as plt
from scipy.stats import shapiro, f_oneway, kruskal
import scikit_posthocs as sp
import os

def load_data(file_path):
    """Load CSV data into a DataFrame."""
    return pd.read_csv(file_path)

def create_output_dir(output_dir):
    """Create output directory if it does not exist."""
    os.makedirs(output_dir, exist_ok=True)

def save_descriptive_statistics(df, file, level="overall"):
    """Save descriptive statistics to the file."""
    desc_stats = df.describe()
    file.write("\n---\n")
    file.write(f"// descriptive_statistics ({level})\n\n")
    file.write(desc_stats.to_string())
    file.write("\n---\n")

def create_boxplot(df, metric, endpoint, output_dir):
    """Create and save a boxplot for the specified metric by implementation."""
    plt.figure(figsize=(12, 6))
    sns.boxplot(x='implementation', y=metric, data=df)
    plt.title(f'{metric.replace("_", " ").title()} by Implementation for {endpoint}')
    plot_path = os.path.join(output_dir, f'boxplot_{metric}_{endpoint}.png')
    plt.savefig(plot_path)
    plt.close()

def perform_normality_tests(df, metrics, file):
    """Perform Shapiro-Wilk test for normality and save results."""
    file.write("// normality_tests\n\n")
    for metric in metrics:
        file.write(f"Normality tests for {metric}:\n")
        for impl in df['implementation'].unique():
            stat, p = shapiro(df[df['implementation'] == impl][metric])
            file.write(f"  Implementation: {impl}, p-value: {p}\n")
        file.write("\n")
    file.write("\n---\n")

def perform_anova_kruskal_tests(df, metrics, file):
    """Perform ANOVA and Kruskal-Wallis tests and save results."""
    file.write("// anova_kruskal_results\n\n")
    results = {}
    for metric in metrics:
        implementations = df['implementation'].unique()
        groups = [df[df['implementation'] == impl][metric] for impl in implementations]
        anova_result = f_oneway(*groups)
        kruskal_result = kruskal(*groups)
        file.write(f"{metric}:\n")
        file.write(f"  ANOVA result: F-statistic = {anova_result.statistic}, p-value = {anova_result.pvalue}\n")
        file.write(f"  Kruskal-Wallis result: H-statistic = {kruskal_result.statistic}, p-value = {kruskal_result.pvalue}\n\n")
        results[metric] = (anova_result, kruskal_result)
    file.write("\n---\n")
    return results

def perform_posthoc_analysis(df, metric, anova_p, kruskal_p, file):
    """Perform post-hoc analysis if ANOVA or Kruskal-Wallis test is significant."""
    if anova_p < 0.05 or kruskal_p < 0.05:
        posthoc = sp.posthoc_dunn(df, val_col=metric, group_col='implementation', p_adjust='bonferroni')
        file.write(f"// posthoc_analysis ({metric})\n\n")
        file.write(posthoc.to_string())
        file.write("\n---\n")
    else:
        file.write(f"// posthoc_analysis ({metric})\n\n")
        file.write(f"{metric}: ANOVA and Kruskal-Wallis tests did not show significant differences, skipping post-hoc analysis.\n")
        file.write("\n---\n")

def analyze_endpoint(df, endpoint, output_file, output_dir):
    """Perform analysis for a specific endpoint."""
    df_endpoint = df[df['endpoint_name'] == endpoint]

    with open(output_file, "a") as file:
        file.write(f"\n---\nEndpoint: {endpoint}\n---\n")

        # Overall Descriptive Statistics
        save_descriptive_statistics(df_endpoint, file, level="overall")

        # Descriptive Statistics by Implementation
        for impl in df_endpoint['implementation'].unique():
            df_impl = df_endpoint[df_endpoint['implementation'] == impl]
            save_descriptive_statistics(df_impl, file, level=impl)

        # Visualization
        metrics = ['req_dur_avg_ms', 'req_dur_p_90_ms', 'req_dur_p_95_ms']
        for metric in metrics:
            create_boxplot(df_endpoint, metric, endpoint, output_dir)

        # Normality Tests
        perform_normality_tests(df_endpoint, metrics, file)

        # ANOVA and Kruskal-Wallis Tests
        results = perform_anova_kruskal_tests(df_endpoint, metrics, file)

        # Post-hoc Analysis
        for metric, (anova_result, kruskal_result) in results.items():
            perform_posthoc_analysis(df_endpoint, metric, anova_result.pvalue, kruskal_result.pvalue, file)

def main():
    # Load data
    print("Step 1: Loading data...")
    df = load_data('k6_performance_data.csv')

    # Create output directory
    output_dir = "analysis_results"
    create_output_dir(output_dir)

    # Open the consolidated output file
    output_file = os.path.join(output_dir, "analysis_results.txt")

    # Analyze each endpoint
    for endpoint in df['endpoint_name'].unique():
        analyze_endpoint(df, endpoint, output_file, output_dir)

    print("Analysis complete. Results have been saved to the 'analysis_results' directory.")

if __name__ == "__main__":
    main()
